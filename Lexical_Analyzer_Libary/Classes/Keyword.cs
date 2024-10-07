using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexical_Analyzer_Libary.Classes
{
    public struct Keyword
    {
        public string Word { get; }
        public Lexems Lex { get; }
        public Keyword(string word, Lexems lexem)
        {
            Word = word;
            Lex = lexem;
        }
    }

}
