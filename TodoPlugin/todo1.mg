module ToDo
{
    language Tasks1 {
        syntax Main = TaskLine+;

        syntax TaskLine = "task" Space+ Title Space+ DueDate EndTask;

        @{Classification["String"]}
        syntax Title = "\"" any* "\"";

        syntax DueDate = Month "-" Day "-" Year;
        
        syntax EndTask = '\r' | '\r''\n' | '\n';
        syntax Space =  ' ';
        
        @{Classification["Keyword"]}
        token Task = "task";
        
        @{Classification["Literal"]}
        token Month = Digit#1..2;
        
        @{Classification["Literal"]}
        token Day = Digit#1..2;

        @{Classification["Literal"]}        
        token Year = Digit#2..4;
        
        @{Classification["Literal"]}        
        token Digit = "0".."9";
    }
}
