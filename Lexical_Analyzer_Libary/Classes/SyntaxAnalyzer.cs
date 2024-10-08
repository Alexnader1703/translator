using Lexical_Analyzer_Libary.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexical_Analyzer_Libary.Classes
{
    /// <summary>
    /// Синтаксический анализатор
    /// </summary>
    public class SyntaxAnalyzer
    {
        private LexicalAnalyzer _lexicalAnalyzer;
        private List<string> _errors;

        public SyntaxAnalyzer(LexicalAnalyzer lexicalAnalyzer)
        {
            _lexicalAnalyzer = lexicalAnalyzer;
            _errors = new List<string>();
        }

        private void Error(string message)
        {
            string errorMessage = $"Ошибка в строке {_lexicalAnalyzer._reader.LineNumber}, позиция {_lexicalAnalyzer._reader.SymbolPositionInLine}: {message}";
            _errors.Add(errorMessage);
        }

        private void CheckLexem(Lexems expectedLexem)
        {
            if (_lexicalAnalyzer.CurrentLexem != expectedLexem)
            {
                Error($"Ожидалась лексема {expectedLexem}, получена {_lexicalAnalyzer.CurrentLexem}");
            }
            else
            {
                _lexicalAnalyzer.ParseNextLexem();
            }
        }

        private void ParseVariableDeclaration()
        {
            // Предполагаем, что тип данных - это идентификатор (Name)
            if (_lexicalAnalyzer.CurrentLexem != Lexems.Name)
            {
                Error("Ожидался тип данных");
                return;
            }
            string dataType = _lexicalAnalyzer.CurrentName;
            _lexicalAnalyzer.ParseNextLexem();

            do
            {
                if (_lexicalAnalyzer.CurrentLexem != Lexems.Name)
                {
                    Error("Ожидался идентификатор");
                    return;
                }

                string identifierName = _lexicalAnalyzer.CurrentName;
                if (_lexicalAnalyzer.GetNameTable().FindByName(identifierName).Name == null)
                {
                    _lexicalAnalyzer.GetNameTable().AddIdentifier(identifierName, tCat.Var);
                }
                else
                {
                    Error($"Повторное объявление идентификатора '{identifierName}'");
                }

                _lexicalAnalyzer.ParseNextLexem();

                if (_lexicalAnalyzer.CurrentLexem == Lexems.Comma)
                {
                    _lexicalAnalyzer.ParseNextLexem();
                }
                else
                {
                    break;
                }
            } while (true);

            CheckLexem(Lexems.Semi); // Ожидаем точку с запятой в конце объявления
        }

        private void ParseBlock()
        {
            CheckLexem(Lexems.Begin);

            while (_lexicalAnalyzer.CurrentLexem != Lexems.End && _lexicalAnalyzer.CurrentLexem != Lexems.EOF)
            {
                ParseStatement();
            }

            CheckLexem(Lexems.End);
        }

        private void ParseStatement()
        {
            switch (_lexicalAnalyzer.CurrentLexem)
            {
                case Lexems.If:
                    ParseIfStatement();
                    break;
                case Lexems.Name:
                    ParseAssignment();
                    break;
                default:
                    Error($"Неожиданная лексема {_lexicalAnalyzer.CurrentLexem}");
                    _lexicalAnalyzer.ParseNextLexem();
                    break;
            }
        }

        private void ParseIfStatement()
        {
            CheckLexem(Lexems.If);
            ParseExpression();
            CheckLexem(Lexems.Then);
            ParseStatement();
            if (_lexicalAnalyzer.CurrentLexem == Lexems.Else)
            {
                _lexicalAnalyzer.ParseNextLexem();
                ParseStatement();
            }
            CheckLexem(Lexems.EndIf);
        }

        private void ParseAssignment()
        {
            CheckLexem(Lexems.Name);
            CheckLexem(Lexems.Assign);
            ParseExpression();
            CheckLexem(Lexems.Semi);
        }

        private void ParseExpression()
        {
            // Упрощенная версия разбора выражений
            while (_lexicalAnalyzer.CurrentLexem == Lexems.Name ||
                   _lexicalAnalyzer.CurrentLexem == Lexems.Number ||
                   _lexicalAnalyzer.CurrentLexem == Lexems.Plus ||
                   _lexicalAnalyzer.CurrentLexem == Lexems.Minus ||
                   _lexicalAnalyzer.CurrentLexem == Lexems.Multiplication ||
                   _lexicalAnalyzer.CurrentLexem == Lexems.Division)
            {
                _lexicalAnalyzer.ParseNextLexem();
            }
        }

        public void Compile()
        {
            _lexicalAnalyzer.ParseNextLexem();
            ParseVariableDeclaration();
            ParseBlock();

            if (_errors.Count > 0)
            {
                Console.WriteLine("Обнаружены ошибки:");
                foreach (var error in _errors)
                {
                    Console.WriteLine(error);
                }
            }
            else
            {
                Console.WriteLine("Синтаксический анализ завершен успешно");
            }
        }
    }
}
