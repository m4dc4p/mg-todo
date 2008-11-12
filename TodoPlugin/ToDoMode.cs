using System.ComponentModel.Composition;
using System.IO;
using Microsoft.Intellipad;
using Microsoft.Intellipad.Host;
using Microsoft.Intellipad.LanguageServices;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using System.Reflection;
using Microsoft.VisualStudio.Text;
using System;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dataflow;
using System.Windows.Threading;
using Microsoft.Intellipad.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace ToDo
{
    /*
     * 1. Implement ToDoMode baed on MGMode
     * 2. Generate source from todo1.mg using "mg -t:Source -o:ToDoPlugin\ todo1.mg"
     *    a. Add todo1.cs and _Anonymous.cs files to prject
     * 3. Implement ILanguageServiceItem based on MGrammarLanguageServiceItem
     * 4. Build to Intellipad Components directory in SDK intsall
     */

    [Export("{Microsoft.Intellipad}Mode")]
    [Export("{Microsoft.Intellipad}ToDoMode")]
    [ExportProperty("{Microsoft.Intellipad}Name", "To-do Mode")]
    [ExportProperty("{Microsoft.Intellipad}Extension", ".todo")]
    public class ToDoMode : Mode
    {
        // boilerplate
        [Import("{Microsoft.Intellipad}StandardMode")]
        public StandardMode StandardMode { get; set; }

        protected override ComponentDomain CreateComponentDomain()
        {
            // boilerplate
            var domain = StandardMode.CreateChildDomain();
            domain.AddComponent(new LanguageServiceItemProvider());
            domain.Bind();
            return domain;
        }

        // boilerplate
        [Export("{Microsoft.Intellipad}LanguageServiceItemProvider")]
        [ComponentOptions(ComponentDiscoveryMode = ComponentDiscoveryMode.Never)]
        class LanguageServiceItemProvider : ILanguageServiceItemProvider
        {

            // boilerplate
            [Import]
            public ISquiggleProviderFactory SquiggleProviderFactory { get; set; }

            public ILanguageServiceItem CreateItem(BufferView bufferView)
            {
                return new ToDoLanguageServiceItem(bufferView, SquiggleProviderFactory);
            }
        }

        class ErrorInfo
        {
            public ISourceLocation Location { get; set; }
            public ErrorInformation Info { get; set; }
        }

        // Mostly stolen from DynamicParserLanguageServiceItem
        class ToDoErrorReporter : ErrorReporter
        {
            public ToDoErrorReporter() 
            {
                Errors = new List<ErrorInfo>();
            }

            protected override void OnError(ISourceLocation sourceLocation, ErrorInformation errorInformation)
            {
                Errors.Add(new ErrorInfo { Location = sourceLocation, Info = errorInformation });
            }

            public List<ErrorInfo> Errors { get; set; }

            public override void Clear()
            {
                Errors.Clear();
                base.Clear();
            }
        }

        class ToDoLanguageServiceItem : ILanguageServiceItem, IDisposable
        {
            BufferView bufferView;
            ParserClassifier classifier;
            Timer reparseTimer;
            DynamicParser parser;
            Uri uri;

            // boilerplate
            ITextBuffer textBuffer;
            volatile bool bufferDirty = false;
            volatile object l = new object();

            ISquiggleProvider squiggleProvider;
            ISquiggleProviderFactory squiggleProviderFactory;
            List<ISquiggleAdornment> squiggles;

            public ToDoLanguageServiceItem(BufferView b, ISquiggleProviderFactory squiggleProviderFactory)
            {
                // Described in MGrammar in a Nutshell (http://msdn.microsoft.com/en-us/library/dd129870.aspx)
                // and in PDC 2008 talk "Building Textual DSLs with the "Oslo" Modeling Language" (32:00 mark).
                //
                parser = null;
                Assembly assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream("ToDo.mgx"))
                {
                    // Load image and instantiate a corresponding dynamic parser
                    parser = Microsoft.M.Grammar.MGrammarCompiler.LoadParserFromMgx(stream, "ToDo.Tasks4");
                }

                this.squiggleProviderFactory = squiggleProviderFactory;
                this.squiggles = new List<ISquiggleAdornment>();

                reparseTimer = new Timer(Reparse, null, Timeout.Infinite, Timeout.Infinite);

                this.bufferView = b;
                this.classifier = new ParserClassifier(parser, bufferView.Buffer.TextBuffer);
                this.textBuffer = bufferView.TextBuffer;
                this.bufferView.EditorInitialized += OnBufferViewEditorInitialized;
                this.uri = this.bufferView.Buffer.Uri;

                this.textBuffer.Changed += (ignore1, ignore2) => { lock (l) { bufferDirty = true; } };
            }

            ~ToDoLanguageServiceItem()
            {
                Dispose();
            }

            void Reparse(object ignored)
            {
                if (bufferDirty)
                {
                    lock (l)
                        if (bufferDirty)
                            bufferDirty = false;

                    // Mostly stolen from DynamicParserLanguageServiceItem
                    ToDoErrorReporter reporter = new ToDoErrorReporter();
                    parser.ParseObject(uri.ToString(), new TextSnapshotToTextReader(this.textBuffer.CurrentSnapshot),
                        reporter);

                    this.bufferView.Dispatcher.BeginInvoke(
                        DispatcherPriority.Normal, (Action)delegate {
                        CleanupOldToolTipPopups();
                        ProcessSquiggles(reporter);
                    });
                }
            }

            // Stolen from DynamicParserLanguageServiceItem
            void CleanupOldToolTipPopups()
            {
                if (this.bufferView.TextEditor != null)
                {
                    ISpaceReservationManager manager = (this.bufferView.TextEditor.TextView as IWpfTextView).GetSpaceReservationManager("ToolTip");
                    if (manager != null)
                    {
                        List<ISpaceReservationAgent> agents = new List<ISpaceReservationAgent>(manager.Agents);
                        foreach (ISpaceReservationAgent toolTipAgent in agents)
                            manager.RemoveAgent(toolTipAgent);
                    }
                }

            }

            // Mostly stolen from DynamicParserLanguageServiceItem
            void ProcessSquiggles(ToDoErrorReporter reporter)
            {
                if (this.squiggleProvider == null)
                    return;

                foreach (ISquiggleAdornment squiggle in this.squiggles)
                    this.squiggleProvider.RemoveSquiggle(squiggle);

                this.squiggles.Clear();

                foreach (ErrorInfo error in reporter.Errors)
                {
                    if (error.Location != null && error.Location.Span.Length > 0)
                    {
                        SnapshotSpan convertedSpan = GetCurrentSpan((ITextSnapshot)this.textBuffer.CurrentSnapshot,
                            error.Location.Span);
                        ITrackingSpan trackingSpan = convertedSpan.Snapshot.CreateTrackingSpan(
                            convertedSpan.Span,
                            SpanTrackingMode.EdgeExclusive);
                        ISquiggleAdornment squiggle = this.squiggleProvider.AddSquiggle(trackingSpan, 
                            String.Format(error.Info.Message, new List<object>(error.Info.Arguments).ToArray()));
                        this.squiggles.Add(squiggle);
                    }
                }
            }

            // Stolen from DynamicParserLanguageServiceItem
            SnapshotSpan GetCurrentSpan(ITextSnapshot snapshot, SourceSpan span)
            {
                ITextSnapshot currentSnapshot = this.bufferView.TextBuffer.CurrentSnapshot;
                // TODO (dougwa): deal with line/column spans
                TextSnapshot cloneSnapshot = new TextSnapshot(snapshot, (TextBufferClone)this.bufferView.TextBuffer);
                SnapshotSpan oldSpan = new SnapshotSpan(cloneSnapshot, span.Start.Index, span.Length);

                SnapshotSpan newSpan = oldSpan.TranslateTo(currentSnapshot, SpanTrackingMode.EdgePositive);
                return newSpan;
            }

            // boilerplate
            void OnBufferViewEditorInitialized(object sender, EventArgs e)
            {
                this.bufferView.EditorInitialized -= OnBufferViewEditorInitialized;
                this.squiggleProvider = this.squiggleProviderFactory.GetSquiggleProvider(this.bufferView.TextEditor.TextView);

                // start looking for errors
                this.reparseTimer.Change(100, 100);
            }

            // boilerplace
            public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
            public event EventHandler<GetCompletionItemsEventArgs> GetCompletionItemsCompleted;
            public IntelliBuffer Buffer { get { return bufferView.Buffer; } }

            // boilerplate
            public IEnumerable<ClassificationItem> GetClassificationItems(SnapshotSpan span)
            {
                return classifier.GetClassificationItems(span);
            }

            // boilerplate
            public void GetCompletionItemsAsync(SnapshotPoint point, object userState)
            {
                if (GetCompletionItemsCompleted != null)
                    GetCompletionItemsCompleted(this, new GetCompletionItemsEventArgs(null, userState));
            }

            // boilerplate
            public ISymbol GetSymbol(SnapshotPoint point) { return null; }

            public void Dispose()
            {
                reparseTimer.Dispose();
                GC.SuppressFinalize(this);
            }
        }
    }
}
