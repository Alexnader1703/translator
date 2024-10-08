using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexical_Analyzer_Libary.Classes
{
    public enum Lexems
    {
        None, Name, Number, Begin, End, If, Then, Else, EndIf, // Добавил недостающие ключевые слова
        Multiplication, Division, Plus, Minus, // Добавил арифметические операторы
        Equal, Greater, Less, LessOrEqual, GreaterOrEqual, // Логические операторы
        Semi, Assign, LeftBracket, RightBracket, EOF, Error,Comma // Завершение оператора и скобки
    }
}
