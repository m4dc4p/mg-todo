module ToDo
{
    // Projecting our tasks into a resonable object
    // model.
    language Tasks4 {
        // Allow an empty task list.
        syntax Main = TaskLine*;
        
        syntax TaskLine = "task" title:Title due:DueDate
            => Task[valuesof(title), valuesof(due)];
            
        // Allow quoted titles        
        syntax Title = SingleQuotedText | DoubleQuotedText;
        // Allow US or international dates.
        syntax DueDate = Date?; 
        // ambiguous, let's default to US.
        syntax Date = 
            // European
            precedence 2: day:Day "-" month:Month "-" year:Year => Date[Month{month}, Year{year}, Day{day}] |
            // US
            precedence 1: month:Month "-" day:Day "-" year:Year => Date[Month{month}, Year{year}, Day{day}];
            
        token Digit = "0".."9";
        token NonZeroDigit = "1".."9";
        token ZeroOrOne  = "0" | "1";
        token Month = ZeroOrOne? NonZeroDigit | NonZeroDigit;
        token Day = Digit#1..2;
        token Year = Digit#2..4;

        token SingleQuotedText = QuotedText('"');
        token DoubleQuotedText = QuotedText("'");
        
        // From M.mg
        token QuotedText(Q) = Q (TextSimple - Q)* Q;
        // No reason to exclude backslash
        token TextSimple = ^('\u000A' // New Line
              | '\u000D' // Carriage Return
              | '\u0085' // Next Line
              | '\u2028' // Line Separator
              | '\u2029'); // Paragraph Separator
        
        // Ignore whitespace
        interleave Whitespace = '\r' | ' ' | '\n';
    }
}
