module ToDo
{
    // Projecting our tasks into a resonable object
    // model.
    language Tasks4 {
        // Allow an empty task list.
        syntax Main = Line*;
    
        syntax Line = TaskLine | Comment;
        syntax TaskLine = "task" Title DueDate?;

        // Allow quoted titles        
        syntax Title = SingleQuotedText | DoubleQuotedText;
        syntax DueDate = Month "-" Day "-" Year;

        // token used here because syntax causes 
        // error. ???
        token Comment = "#" ^('\r' | '\n')*;
        
        token Digit = "0".."9";
        token Month = Digit#1..2;
        token Day = Digit#1..2;
        token Year = Digit#2..4;

        token SingleQuotedText = QuotedText('"');
        token DoubleQuotedText = QuotedText("'");
        
        // Parameterized rule.        
        token QuotedText(Q) = Q (Text - Q)* Q;
        
        // Inspired by M.mg
        token Text = '"' ^('\n' | '\r')* '"'; 
        
        // Ignore whitespace
        interleave Whitespace = '\r' | ' ' | '\n';
    }
}
