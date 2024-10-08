using Lexical_Analyzer_Libary.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    internal class Program
    {
        static void Main(string[] args)
        {
            
           
            LexicalAnalyzer analyzer = new LexicalAnalyzer(@"C:\Users\Julia\Documents\GitHub\translator\Lexical_Analyzer_Libary\Assets\inp.txt");
            while (analyzer.CurrentLexem != Lexems.EOF)
            {
                analyzer.ParseNextLexem();
               
            }
            List<string> lexemes = analyzer.GetLexemes();
            Console.WriteLine("\nВсе лексемы в виде списка строк:");
            foreach (var lexeme in lexemes)
            {
                Console.WriteLine(lexeme);
            }

            Console.ReadKey();
        }

       
       

       
    }
}
