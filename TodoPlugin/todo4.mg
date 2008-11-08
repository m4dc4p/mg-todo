module ToDo
{
    // Projecting our tasks into a resonable object
    // model.
    language Tasks4 {
        // Allow an empty task list.
        syntax Main = Line*;
    
        syntax Line = TaskLine | Comment;
        syntax TaskLine = Task Title DueDate?;

        // Allow quoted titles        
        syntax Title = SingleQuotedText | DoubleQuotedText;
        syntax DueDate = Month "-" Day "-" Year;

        @{Classification["Keyword"]}
        token Task = "task";
        
        // token used here because syntax causes 
        // error. ???
        @{Classification["Comment"]}
        token Comment = "#" ^('\r' | '\n')*;
        
        @{Classification["Literal"]}
        token Digit = "0".."9";
        @{Classification["Literal"]}
        token Month = Digit#1..2;
        @{Classification["Literal"]}
        token Day = Digit#1..2;
        @{Classification["Literal"]}
        token Year = Digit#2..4;

        @{Classification["String"]}
        token SingleQuotedText = QuotedText('"');
        @{Classification["String"]}
        token DoubleQuotedText = QuotedText("'");
        
        // Parameterized rule.        
        token QuotedText(Q) = Q (Text - Q)* Q;
        
        // Inspired by M.mg
        token Text = '"' ^('\n' | '\r')* '"'; 
        
        // Ignore whitespace
        @{Classification["Whitespace"]}
        interleave Whitespace = '\r' | ' ' | '\n';
    }
}
