module ToDo
{
    language Tasks1 {
        syntax Main = TaskLine+;

        syntax TaskLine = "task" Space+ Title Space+ DueDate NewLine;
        syntax Title = "\"" any* "\"";
        syntax DueDate = Month "-" Day "-" Year;
        
        token Digit = "0".."9";
        token Month = Digit#1..2;
        token Day = Digit#1..2;
        token Year = Digit#2..4;
        token NewLine = '\r' | '\r''\n' | '\n';
        
        token Space =  ' ';
    }
}
