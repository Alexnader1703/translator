using System;
using System.Collections.Generic;
using System.Text;

namespace Lexical_Analyzer_Libary.Classes
{
    public class LexicalAnalyzer
    {
        public Reader _reader;
        private Keyword[] _keywords;
        private readonly HashSet<string> _types;
        private int _keywordsPointer;
        private List<string> _lexemes;
        private NameTable _nameTable;
        private List<string> _errors;

        public Lexems CurrentLexem { get; private set; }
        public string CurrentName { get; private set; }
        public int CurrentNumber { get; private set; }
        public bool HasErrors => _errors.Count > 0;
        public IReadOnlyList<string> Errors => _errors;

        public LexicalAnalyzer(string input, bool isStringInput = false)
        {
            _keywords = new Keyword[30]; // Увеличил размер массива для большего количества ключевых слов
            _keywordsPointer = 0;
            _lexemes = new List<string>();
            _errors = new List<string>();
            _types = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "int", "string", "bool", "float", "double", "char", "void"
    };

            InitializeKeywords();

            if (isStringInput)
            {
                _reader = new Reader(input, true); // Используем строку в качестве входных данных
            }
            else
            {
                _reader = new Reader(input); // Используем путь к файлу
            }

            _nameTable = new NameTable();
            CurrentLexem = Lexems.None;
        }

        private void InitializeKeywords()
        {
            // Основные ключевые слова
            AddKeyword("begin", Lexems.Begin);
            AddKeyword("end", Lexems.End);
            AddKeyword("if", Lexems.If);
            AddKeyword("then", Lexems.Then);
            AddKeyword("Then", Lexems.Then);
            AddKeyword("else", Lexems.Else);
            AddKeyword("endif", Lexems.EndIf);
            AddKeyword("while", Lexems.While);
            AddKeyword("endwhile", Lexems.EndWhile);
            AddKeyword("elseif", Lexems.ElseIf);
            AddKeyword("do", Lexems.Do);
            AddKeyword("for", Lexems.For);
            AddKeyword("print", Lexems.Print);
            AddKeyword("case", Lexems.Case);
            AddKeyword("of", Lexems.Of);
            AddKeyword("endcase", Lexems.EndCase);
            AddKeyword("true", Lexems.True);
            AddKeyword("false", Lexems.False);

        }

        private void AddKeyword(string keyword, Lexems lexem)
        {
            if (_keywordsPointer < _keywords.Length)
            {
                _keywords[_keywordsPointer++] = new Keyword(keyword, lexem);
            }
            else
            {
                throw new InvalidOperationException("Превышен лимит ключевых слов");
            }
        }

        private void AddError(string message)
        {
            _errors.Add($"Ошибка в строке {_reader.LineNumber}, позиция {_reader.SymbolPositionInLine}: {message}");
        }

        private Lexems GetKeywordLexem(string word)
        {
            // Проверяем, является ли слово типом данных
            if (_types.Contains(word))
            {
                return Lexems.Type;
            }

            // Проверяем, является ли слово ключевым словом
            for (int i = 0; i < _keywordsPointer; i++)
            {
                if (_keywords[i].Word.Equals(word, StringComparison.OrdinalIgnoreCase))
                    return _keywords[i].Lex;
            }
            return Lexems.Name;
        }

        public void ParseNextLexem()
        {
            try
            {
                SkipWhitespaceAndComments();

                if (_reader.CurrentSymbol == '\0')
                {
                    CurrentLexem = Lexems.EOF;
                    return;
                }

                if (char.IsLetter((char)_reader.CurrentSymbol) || _reader.CurrentSymbol == '_')
                {
                    ParseIdentifier();
                }
                else if (char.IsDigit((char)_reader.CurrentSymbol))
                {
                    ParseNumber();
                }
                else if (_reader.CurrentSymbol == '"')
                {
                    ParseString();
                }
                else if (_reader.CurrentSymbol == '\'')
                {
                    ParseChar();
                }
                else
                {
                    ParseSpecialSymbol();
                }

                _lexemes.Add($"{CurrentLexem}" + (CurrentLexem == Lexems.Name || CurrentLexem == Lexems.Type ?
                             $"({CurrentName})" : CurrentLexem == Lexems.Number ? $"({CurrentNumber})" : ""));
            }
            catch (Exception ex)
            {
                AddError($"Неожиданная ошибка при разборе: {ex.Message}");
                CurrentLexem = Lexems.Error;
            }
        }

        private void SkipWhitespaceAndComments()
        {
            while (true)
            {
                // Пропуск пробельных символов
                while (char.IsWhiteSpace((char)_reader.CurrentSymbol))
                {
                    _reader.ReadNextSymbol();
                }

                // Проверка на комментарии
                if (_reader.CurrentSymbol == '&')
                {
                    _reader.ReadNextSymbol();

                    if (_reader.CurrentSymbol == '&') // Однострочный комментарий
                    {
                        while (_reader.CurrentSymbol != '\n' && _reader.CurrentSymbol != '\0')
                        {
                            _reader.ReadNextSymbol();
                        }
                    }
                    else if (_reader.CurrentSymbol == '*') // Многострочный комментарий
                    {
                        bool commentClosed = false;
                        _reader.ReadNextSymbol();

                        while (_reader.CurrentSymbol != '\0')
                        {
                            if (_reader.CurrentSymbol == '*')
                            {
                                _reader.ReadNextSymbol();
                                if (_reader.CurrentSymbol == '&')
                                {
                                    commentClosed = true;
                                    _reader.ReadNextSymbol();
                                    break;
                                }
                            }
                            else
                            {
                                _reader.ReadNextSymbol();
                            }
                        }

                        if (!commentClosed)
                        {
                            AddError("Незакрытый многострочный комментарий");
                        }
                    }
                    else
                    {
                        // Это не комментарий, а оператор деления
                        //_reader.MoveBack();
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        private void ParseIdentifier()
        {
            StringBuilder identifier = new StringBuilder();

            // Разрешаем идентификаторам начинаться с подчеркивания
            if (_reader.CurrentSymbol == '_')
            {
                identifier.Append((char)_reader.CurrentSymbol);
                _reader.ReadNextSymbol();
            }

            while (char.IsLetterOrDigit((char)_reader.CurrentSymbol) || _reader.CurrentSymbol == '_')
            {
                identifier.Append((char)_reader.CurrentSymbol);
                _reader.ReadNextSymbol();
            }

            CurrentName = identifier.ToString();

            if (string.IsNullOrEmpty(CurrentName))
            {
                AddError("Пустой идентификатор");
                CurrentLexem = Lexems.Error;
                return;
            }

            if (CurrentName.Length > 255)
            {
                AddError("Идентификатор слишком длинный");
                CurrentLexem = Lexems.Error;
                return;
            }

            CurrentLexem = GetKeywordLexem(CurrentName);

          
        }

        private void ParseNumber()
        {
            StringBuilder number = new StringBuilder();
            bool hasDecimalPoint = false;

            while (char.IsDigit((char)_reader.CurrentSymbol) || _reader.CurrentSymbol == '.')
            {
                if (_reader.CurrentSymbol == '.')
                {
                    if (hasDecimalPoint)
                    {
                        AddError("Множественные десятичные точки в числе");
                        CurrentLexem = Lexems.Error;
                        return;
                    }
                    hasDecimalPoint = true;
                }

                number.Append((char)_reader.CurrentSymbol);
                _reader.ReadNextSymbol();
            }

            // Проверяем, не является ли следующий символ буквой (недопустимый формат числа)
            if (char.IsLetter((char)_reader.CurrentSymbol))
            {
                AddError("Недопустимый формат числа");
                CurrentLexem = Lexems.Error;
                return;
            }

            if (int.TryParse(number.ToString(), out int result))
            {
                CurrentNumber = result;
                CurrentLexem = Lexems.Number;
            }
            else
            {
                AddError("Число вне допустимого диапазона");
                CurrentLexem = Lexems.Error;
            }
        }

        private void ParseString()
        {
            StringBuilder str = new StringBuilder();
            _reader.ReadNextSymbol(); // Пропускаем открывающую кавычку

            while (_reader.CurrentSymbol != '"' && _reader.CurrentSymbol != '\0')
            {
                if (_reader.CurrentSymbol == '\\')
                {
                    _reader.ReadNextSymbol();
                    switch (_reader.CurrentSymbol)
                    {
                        case 'n': str.Append('\n'); break;
                        case 't': str.Append('\t'); break;
                        case 'r': str.Append('\r'); break;
                        case '\\': str.Append('\\'); break;
                        case '"': str.Append('"'); break;
                        default:
                            AddError("Неизвестная escape-последовательность");
                            break;
                    }
                }
                else
                {
                    str.Append((char)_reader.CurrentSymbol);
                }
                _reader.ReadNextSymbol();
            }

            if (_reader.CurrentSymbol == '"')
            {
                _reader.ReadNextSymbol();
                CurrentName = str.ToString();
                CurrentLexem = Lexems.String;
            }
            else
            {
                AddError("Незакрытая строка");
                CurrentLexem = Lexems.Error;
            }
        }

        private void ParseChar()
        {
            StringBuilder ch = new StringBuilder();
            _reader.ReadNextSymbol(); // Пропускаем открывающую кавычку

            if (_reader.CurrentSymbol == '\\')
            {
                _reader.ReadNextSymbol();
                switch (_reader.CurrentSymbol)
                {
                    case 'n': ch.Append('\n'); break;
                    case 't': ch.Append('\t'); break;
                    case 'r': ch.Append('\r'); break;
                    case '\\': ch.Append('\\'); break;
                    case '\'': ch.Append('\''); break;
                    default:
                        AddError("Неизвестная escape-последовательность в символе");
                        break;
                }
                _reader.ReadNextSymbol();
            }
            else
            {
                ch.Append((char)_reader.CurrentSymbol);
                _reader.ReadNextSymbol();
            }

            if (_reader.CurrentSymbol == '\'')
            {
                _reader.ReadNextSymbol();
                CurrentName = ch.ToString();
                CurrentLexem = Lexems.Char;
            }
            else
            {
                AddError("Незакрытый символьный литерал");
                CurrentLexem = Lexems.Error;
            }
        }

        private void ParseSpecialSymbol()
        {
            char currentChar = (char)_reader.CurrentSymbol;
            _reader.ReadNextSymbol();

            switch (currentChar)
            {
                case '+':
                    if (_reader.CurrentSymbol == '=')
                    {
                        CurrentLexem = Lexems.PlusAssign;
                        _reader.ReadNextSymbol();
                    }
                    else if (_reader.CurrentSymbol == '+')
                    {
                        CurrentLexem = Lexems.Increment;
                        _reader.ReadNextSymbol();
                    }
                    else
                    {
                        CurrentLexem = Lexems.Plus;
                    }
                    break;

                case '-':
                    if (_reader.CurrentSymbol == '=')
                    {
                        CurrentLexem = Lexems.MinusAssign;
                        _reader.ReadNextSymbol();
                    }
                    else if (_reader.CurrentSymbol == '-')
                    {
                        CurrentLexem = Lexems.Decrement;
                        _reader.ReadNextSymbol();
                    }
                    else
                    {
                        CurrentLexem = Lexems.Minus;
                    }
                    break;

                case '*':
                    if (_reader.CurrentSymbol == '=')
                    {
                        CurrentLexem = Lexems.MultiplyAssign;
                        _reader.ReadNextSymbol();
                    }
                    else
                    {
                        CurrentLexem = Lexems.Multiplication;
                    }
                    break;

                case '/':
                    if (_reader.CurrentSymbol == '=')
                    {
                        CurrentLexem = Lexems.DivideAssign;
                        _reader.ReadNextSymbol();
                    }
                    else
                    {
                        CurrentLexem = Lexems.Division;
                    }
                    break;

                case '=':
                    if (_reader.CurrentSymbol == '=')
                    {
                        CurrentLexem = Lexems.Equal;
                        _reader.ReadNextSymbol();
                    }
                    else
                    {
                        CurrentLexem = Lexems.Assign;
                    }
                    break;

                case '!':
                    if (_reader.CurrentSymbol == '=')
                    {
                        CurrentLexem = Lexems.NotEqual;
                        _reader.ReadNextSymbol();
                    }
                    else
                    {
                        CurrentLexem = Lexems.Not;
                    }
                    break;
                // Продолжение метода ParseSpecialSymbol():
                case '<':
                    if (_reader.CurrentSymbol == '=')
                    {
                        CurrentLexem = Lexems.LessOrEqual;
                        _reader.ReadNextSymbol();
                    }
                    else if (_reader.CurrentSymbol == '>')
                    {
                        CurrentLexem = Lexems.NotEqual;
                        _reader.ReadNextSymbol();
                    }
                    else
                    {
                        CurrentLexem = Lexems.Less;
                    }
                    break;

                case '>':
                    if (_reader.CurrentSymbol == '=')
                    {
                        CurrentLexem = Lexems.GreaterOrEqual;
                        _reader.ReadNextSymbol();
                    }
                    else
                    {
                        CurrentLexem = Lexems.Greater;
                    }
                    break;
                case ':':
                    CurrentLexem = Lexems.Colon;
                    break;

                case ';':
                    CurrentLexem = Lexems.Semi;
                    break;

                case ',':
                    CurrentLexem = Lexems.Comma;
                    break;

                case '(':
                    CurrentLexem = Lexems.LeftPar;
                    break;

                case ')':
                    CurrentLexem = Lexems.RightPar;
                    break;

                default:
                    AddError($"Неизвестный символ: {currentChar}");
                    CurrentLexem = Lexems.Error;
                    break;
            }
        }



        // Метод для возврата всех найденных лексем
        public List<string> GetLexemes() => _lexemes;

        // Метод для получения таблицы имен
        public NameTable GetNameTable() => _nameTable;

        // Метод для проверки наличия ошибок
        public bool HasLexicalErrors()
        {
            return _errors.Count > 0;
        }

        // Метод для получения всех ошибок
        public IEnumerable<string> GetErrors()
        {
            return _errors;
        }

       
    }
}


