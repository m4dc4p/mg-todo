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

        class ToDoLanguageServiceItem : ILanguageServiceItem
        {
            BufferView bufferView;
            ParserClassifier classifier;

            // boilerplate
            ISquiggleProvider squiggleProvider;
            ISquiggleProviderFactory squiggleProviderFactory;
            System.Threading.Timer reclassifyTimer;
            ITextBuffer textBuffer;

            public ToDoLanguageServiceItem(BufferView b, ISquiggleProviderFactory squiggleProviderFactory)
            {
                // Described in MGrammar in a Nutshell (http://msdn.microsoft.com/en-us/library/dd129870.aspx)
                // and in PDC 2008 talk "Building Textual DSLs with the "Oslo" Modeling Language" (32:00 mark).
                //
                System.Dataflow.DynamicParser parser = null;
                Assembly assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream("ToDo.mgx"))
                {
                    // Load image and instantiate a corresponding dynamic parser
                    parser = Microsoft.M.Grammar.MGrammarCompiler.LoadParserFromMgx(stream, "ToDo.Tasks4");
                }

                // boilerplate
                bufferView = b;
                this.classifier = new ParserClassifier(parser, bufferView.Buffer.TextBuffer);
                this.squiggleProviderFactory = squiggleProviderFactory;
                this.textBuffer = bufferView.TextBuffer;

                this.reclassifyTimer = new System.Threading.Timer((o) =>
                {
                    if (ClassificationChanged != null && classifier != null)
                    {
                        SnapshotSpan span = classifier.Reclassify(this.textBuffer.CurrentSnapshot);
                        NotifyClassificationChanged(span);
                    }

                }, null, -1, -1);

                this.textBuffer.Changed += (o, e) =>
                {
                    // TODO (dougwa): the value here (50) should be configurable.
                    this.reclassifyTimer.Change(50, -1);
                };

                this.bufferView.EditorInitialized += OnBufferViewEditorInitialized;
            }

            // boilerplate
            void OnBufferViewEditorInitialized(object sender, EventArgs e)
            {
                this.bufferView.EditorInitialized -= OnBufferViewEditorInitialized;
                this.squiggleProvider = this.squiggleProviderFactory.GetSquiggleProvider(this.bufferView.TextEditor.TextView);
            }

            // boilerplate
            void NotifyClassificationChanged(SnapshotSpan span)
            {
                if (ClassificationChanged != null)
                {
                    ClassificationChanged(this, new ClassificationChangedEventArgs(span));
                }
            }

            public IntelliBuffer Buffer
            {
                get { return bufferView.Buffer; }
            }

            public event System.EventHandler<Microsoft.VisualStudio.Text.Classification.ClassificationChangedEventArgs> ClassificationChanged;

            // boilerplate
            public System.Collections.Generic.IEnumerable<ClassificationItem> GetClassificationItems(Microsoft.VisualStudio.Text.SnapshotSpan span)
            {
                return classifier.GetClassificationItems(span);
            }

            // boilerplate
            public void GetCompletionItemsAsync(Microsoft.VisualStudio.Text.SnapshotPoint point, object userState)
            {
                if (GetCompletionItemsCompleted != null)
                    GetCompletionItemsCompleted(this, new GetCompletionItemsEventArgs(null, userState));
            }

            public event System.EventHandler<GetCompletionItemsEventArgs> GetCompletionItemsCompleted;

            public ISymbol GetSymbol(Microsoft.VisualStudio.Text.SnapshotPoint point)
            {
                // boilerplate
                return null;
            }
        }
    }
}
