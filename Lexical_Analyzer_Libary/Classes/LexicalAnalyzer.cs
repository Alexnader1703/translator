using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexical_Analyzer_Libary.Classes
{

    /// <summary>
    /// Лексический анализатор
    /// </summary>
    public class LexicalAnalyzer
    {
        private Reader _reader;
        private Keyword[] _keywords;
        private int _keywordsPointer;
        private List<string> _lexemes;

        public Lexems CurrentLexem { get; private set; }
        public string CurrentName { get; private set; }
        public int CurrentNumber { get; private set; }

        public LexicalAnalyzer(string filePath)
        {
            _keywords = new Keyword[20];
            _keywordsPointer = 0;
            _lexemes = new List<string>();
            InitializeKeywords();
            _reader = new Reader(filePath);
            CurrentLexem = Lexems.None;
        }


        private void InitializeKeywords()
        {
            AddKeyword("begin", Lexems.Begin);
            AddKeyword("end", Lexems.End);
            AddKeyword("if", Lexems.If);
            AddKeyword("then", Lexems.Then);
            AddKeyword("else", Lexems.Else);       
            AddKeyword("endif", Lexems.EndIf);
            AddKeyword(",", Lexems.Comma);
        }

        private void AddKeyword(string keyword, Lexems lexem)
        {
            if (_keywordsPointer < _keywords.Length)
            {
                _keywords[_keywordsPointer++] = new Keyword(keyword, lexem);
            }
        }

        private Lexems GetKeywordLexem(string word)
        {
            for (int i = 0; i < _keywordsPointer; i++)
            {
                if (_keywords[i].Word == word)
                    return _keywords[i].Lex;
            }
            return Lexems.Name;
        }

        public void ParseNextLexem()
        {
            // Пропускаем пробелы и управляющие символы
            while (_reader.CurrentSymbol == ' ' || _reader.CurrentSymbol == '\t' || _reader.CurrentSymbol == '\r' || _reader.CurrentSymbol == '\n')
            {
                _reader.ReadNextSymbol();
            }

            // Обработка идентификаторов и ключевых слов
            if (char.IsLetter((char)_reader.CurrentSymbol))
            {
                ParseIdentifier();
            }
            // Обработка чисел
            else if (char.IsDigit((char)_reader.CurrentSymbol))
            {
                ParseNumber();
            }
            else
            {
                switch (_reader.CurrentSymbol)
                {
                    case '+':
                        _reader.ReadNextSymbol();
                        CurrentLexem = Lexems.Plus;
                        break;
                    case '-':
                        _reader.ReadNextSymbol();
                        CurrentLexem = Lexems.Minus;
                        break;
                    case '*':
                        _reader.ReadNextSymbol();
                        CurrentLexem = Lexems.Multiplication;
                        break;
                    case '/':
                        _reader.ReadNextSymbol();
                        CurrentLexem = Lexems.Division;
                        break;
                    case '=':
                        _reader.ReadNextSymbol();
                        CurrentLexem = Lexems.Equal;
                        break;
                    case '<':
                        _reader.ReadNextSymbol();
                        if (_reader.CurrentSymbol == '=')
                        {
                            _reader.ReadNextSymbol();
                            CurrentLexem = Lexems.LessOrEqual;
                        }
                        else
                        {
                            CurrentLexem = Lexems.Less;
                        }
                        break;
                    case '>':
                        _reader.ReadNextSymbol();
                        if (_reader.CurrentSymbol == '=')
                        {
                            _reader.ReadNextSymbol();
                            CurrentLexem = Lexems.GreaterOrEqual;
                        }
                        else
                        {
                            CurrentLexem = Lexems.Greater;
                        }
                        break;
                    case '(':
                        _reader.ReadNextSymbol();
                        CurrentLexem = Lexems.LeftBracket;
                        break;
                    case ')':
                        _reader.ReadNextSymbol();
                        CurrentLexem = Lexems.RightBracket;
                        break;
                    case ';':
                        _reader.ReadNextSymbol();
                        CurrentLexem = Lexems.Semi;
                        break;
                    case ',':
                        _reader.ReadNextSymbol();
                        CurrentLexem = Lexems.Comma;
                        break;
                    case '\0':
                        CurrentLexem = Lexems.EOF;
                        break;
                    default:
                        // Неопознанный символ
                        CurrentLexem = Lexems.EOF;
                        break;
                }
            }

            _lexemes.Add(CurrentLexem.ToString());
        }
        private void ParseIdentifier()
        {
            string identifier = string.Empty;
            while (char.IsLetter((char)_reader.CurrentSymbol))
            {
                identifier += (char)_reader.CurrentSymbol;
                _reader.ReadNextSymbol();
            }

            CurrentName = identifier;
            CurrentLexem = GetKeywordLexem(identifier);
        }

        private void ParseNumber()
        {
            string number = string.Empty;
            while (char.IsDigit((char)_reader.CurrentSymbol))
            {
                number += _reader.CurrentSymbol;
                _reader.ReadNextSymbol();
            }

            CurrentNumber = int.Parse(number);
            CurrentLexem = Lexems.Number;
        }

        public List<string> GetLexemes()
        {
            return _lexemes;
        }
    }
}
