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
            
           
            LexicalAnalyzer analyzer = new LexicalAnalyzer(@"D:\Github\translator\Lexical_Analyzer_Libary\Assets\inp.txt");
            RunAnalysis(analyzer);

           
            Console.ReadKey();
        }

        /// <summary>
        /// Выполняет разбор входного текста и выводит результаты на экран
        /// </summary>
        /// <param name="analyzer">Экземпляр лексического анализатора</param>
        static void RunAnalysis(LexicalAnalyzer analyzer)
        {
            // Выполняем разбор всех лексем в строке
            while (analyzer.CurrentLexem != Lexems.EOF && analyzer.CurrentLexem != Lexems.Error)
            {
                analyzer.ParseNextLexem();
                PrintCurrentLexem(analyzer);
            }
        }

        /// <summary>
        /// Печатает текущую лексему на экран
        /// </summary>
        /// <param name="analyzer">Экземпляр лексического анализатора</param>
        static void PrintCurrentLexem(LexicalAnalyzer analyzer)
        {
            switch (analyzer.CurrentLexem)
            {
                case Lexems.Begin:
                    Console.WriteLine($"Лексема: BEGIN, Имя: {analyzer.CurrentName}");
                    break;
                case Lexems.End:
                    Console.WriteLine($"Лексема: END, Имя: {analyzer.CurrentName}");
                    break;
                case Lexems.If:
                    Console.WriteLine($"Лексема: IF, Имя: {analyzer.CurrentName}");
                    break;
                case Lexems.Then:
                    Console.WriteLine($"Лексема: THEN, Имя: {analyzer.CurrentName}");
                    break;
                case Lexems.Name:
                    Console.WriteLine($"Идентификатор: {analyzer.CurrentName}");
                    break;
                case Lexems.Number:
                    Console.WriteLine($"Число: {analyzer.CurrentNumber}");
                    break;
                case Lexems.Plus:
                    Console.WriteLine("Лексема: PLUS");
                    break;
                case Lexems.Equal:
                    Console.WriteLine("Лексема: EQUAL");
                    break;
                case Lexems.EOF:
                    Console.WriteLine("Конец файла (EOF)");
                    break;
                case Lexems.Error:
                    Console.WriteLine("Ошибка: неизвестная лексема");
                    break;
                default:
                    Console.WriteLine("Неизвестная лексема");
                    break;
            }
        }
    }
}
