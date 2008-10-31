module ToDo
{
    language Tasks2 {
        syntax Main = TaskLine+;

        // Separators and newline removed since 
        // we are using interleave.
        syntax TaskLine = "task" Title DueDate;
        syntax DueDate = Month "-" Day "-" Year;
        token Title = '"' ^('"' | '\n' | '\r')* '"'; 

        token Digit = "0".."9";
        token Month = Digit#1..2;
        token Day = Digit#1..2;
        token Year = Digit#2..4;
        
        interleave Whitespace = '\r' | ' ' | '\n';
    }
}
