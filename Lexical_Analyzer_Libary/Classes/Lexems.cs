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
        Type,           // Тип данных (например, int, string)

        // Ключевые слова
        Begin,          // begin
        End,            // end
        If,             // if
        Then,           // then
        Else,           // else
        ElseIf,         // elseif
        EndIf,          // endif
        Do,             // do
        While,          // while
        EndWhile,       // endwhile
        For,            // for
        Case,           // case
        Of,             // of
        EndCase,        // endcase
        Colon,          // :
        True,
        False,

        // Арифметические операторы
        Plus,           // +
        Minus,          // -
        Multiplication, // *
        Division,       // /
        MultiplyAssign, // *=
        DivideAssign,   // /=
        MinusAssign,    // -=
        PlusAssign,     // +=
        Decrement,      // --
        Increment,      // ++

        // Операторы сравнения
        Equal,          // ==
        NotEqual,       // !=
        Greater,        // >
        Less,           // <
        GreaterOrEqual, // >=
        LessOrEqual,    // <=
        Not,            // !

        // Специальные символы
        Assign,         // =
        Semi,           // ;
        Comma,          // ,
        LeftPar,        // (
        RightPar,       // )
        Char,           // Символ
        String,         // Строка

        // Служебные лексемы
        EOF,            // Конец файла
        Error,          // Ошибка
        Print           // print
    }
}
