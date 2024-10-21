using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexical_Analyzer_Libary.Classes
{
    public enum Lexems
    {
        None,           // Нет лексемы
        Name,           // Идентификатор
        Number,         // Число
        Type,           // Тип данных (int, string и т.д.)
        // Ключевые слова
        Begin,          // begin
        End,            // end
        If,             // if
        Then,           // then
        Else,           // else
        EndIf,          // endif
        Do,             // do
        While,For,          // while
        // Арифметические операторы
        Plus,           // +
        Minus,          // -
        Multiplication, // *
        Division, MultiplyAssign, DivideAssign, MinusAssign, PlusAssign, Decrement, Increment,       // /
        // Операторы сравнения
        Equal,          // ==
        NotEqual,       // !=
        Greater,        // >
        Less,           // <
        GreaterOrEqual, // >=
        LessOrEqual,  
        Not,// <=
        // Специальные символы
        Assign,         // =
        Semi,           // ;
        Comma,          // ,
        LeftPar,        // (
        RightPar, Char,String,      // )
        // Служебные лексемы
        EOF,            // Конец файла
        Error           // Ошибка
    }

}
