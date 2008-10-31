module ToDo
{
    import Language;
    
    language Tasks1 {
        // Allow empty file
        syntax Main = TaskLine*;

        // Separators and newline removed since 
        // we are using interleave.
        syntax TaskLine = "task" Title DueDate;
        syntax Title = Text;
        syntax DueDate = Month "-" Day "-" Year;

        token Digit = "0".."9";
        token Month = Digit#1..2;
        token Day = Digit#1..2;
        token Year = Digit#2..4;
        token NewLine = '\r' | '\r''\n' | '\n';

        // Inspired by M.mg
        token Text = '"' ^('"' | '\n' | '\r')* '"'; 
        
        // Ignore whitespace
        interleave Whitespace = '\r' | ' ' | '\n';
    }
}
