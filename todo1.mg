module ToDo
{
    language Tasks1 {
        syntax Main = TaskLine+;

        syntax TaskLine = "task" Space+ Title Space+ DueDate EndTask;
        syntax Title = "\"" any* "\"";
        syntax DueDate = Month "-" Day "-" Year;
        
        syntax EndTask = '\r' | '\r''\n' | '\n';
        syntax Space =  ' ';
        
        token Month = Digit#1..2;
        token Day = Digit#1..2;
        token Year = Digit#2..4;
        token Digit = "0".."9";
    }
}
