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


            string filePath = @"C:\Users\Julia\Documents\GitHub\translator\Lexical_Analyzer_Libary\Assets\inp.txt";

            // Создаем экземпляр лексического анализатора
            LexicalAnalyzer lexicalAnalyzer = new LexicalAnalyzer(filePath);

            // Создаем экземпляр синтаксического анализатора
            SyntaxAnalyzer syntaxAnalyzer = new SyntaxAnalyzer(lexicalAnalyzer);

            // Запускаем компиляцию (синтаксический анализ)
            syntaxAnalyzer.Compile();

            // Выводим все лексемы 
            List<string> lexemes = lexicalAnalyzer.GetLexemes();
            Console.WriteLine("\nВсе лексемы в виде списка строк:");
            foreach (var lexeme in lexemes)
            {
                Console.WriteLine(lexeme);
            }

            // Выводим все идентификаторы в таблице имен 
            Console.WriteLine("\nВсе идентификаторы в таблице имен:");
            foreach (var identifier in lexicalAnalyzer.GetNameTable().GetIdentifiers())
            {
                Console.WriteLine(identifier);
            }

            Console.ReadKey();
        }

       
       

       
    }
}
