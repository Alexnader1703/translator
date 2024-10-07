using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexical_Analyzer_Libary.Classes
{
    public enum Lexems
    {
        None, Name, Number, Begin, End, If, Then, Multiplication, Division, Plus,
        Equal, Less, LessOrEqual, Semi, Assign, LeftBracket, EOF, Error
    }
}
