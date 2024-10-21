using Lexical_Analyzer_Libary.Classes;
using System;
using System.Collections.Generic;

namespace Lexical_Analyzer_Libary.Classes
{
    public class SyntaxAnalyzer
    {
        private LexicalAnalyzer _lexicalAnalyzer;
        private List<string> _errors;

        public SyntaxAnalyzer(LexicalAnalyzer lexicalAnalyzer)
        {
            _lexicalAnalyzer = lexicalAnalyzer;
            _errors = new List<string>();
        }

        private void ParseStatementSequence()
        {
            while (true)
            {
                // Проверяем наличие конца блока
                if (_lexicalAnalyzer.CurrentLexem == Lexems.End ||
                    _lexicalAnalyzer.CurrentLexem == Lexems.EOF ||
                    _lexicalAnalyzer.CurrentLexem == Lexems.EndIf ||
                    _lexicalAnalyzer.CurrentLexem == Lexems.Else) // Добавляем проверку на Else
                {
                    break; // Конец блока или файла
                }

                ParseStatement();

                // Проверка точки с запятой после каждого оператора, кроме блоков if и else
                if (_lexicalAnalyzer.CurrentLexem == Lexems.Semi)
                {
                    _lexicalAnalyzer.ParseNextLexem();
                }
                else if (_lexicalAnalyzer.CurrentLexem != Lexems.End &&
                         _lexicalAnalyzer.CurrentLexem != Lexems.EOF &&
                         _lexicalAnalyzer.CurrentLexem != Lexems.EndIf &&
                         _lexicalAnalyzer.CurrentLexem != Lexems.Else) // Исключаем Else и EndIf из проверки
                {
                    Error("Ожидается точка с запятой");
                    // Пропуск до следующей точки с запятой или конца блока
                    while (_lexicalAnalyzer.CurrentLexem != Lexems.Semi &&
                           _lexicalAnalyzer.CurrentLexem != Lexems.End &&
                           _lexicalAnalyzer.CurrentLexem != Lexems.EOF &&
                           _lexicalAnalyzer.CurrentLexem != Lexems.EndIf &&
                           _lexicalAnalyzer.CurrentLexem != Lexems.Else) // Исключаем Else и EndIf
                    {
                        _lexicalAnalyzer.ParseNextLexem();
                    }

                    // Если нашли точку с запятой, считываем следующую лексему
                    if (_lexicalAnalyzer.CurrentLexem == Lexems.Semi)
                    {
                        _lexicalAnalyzer.ParseNextLexem();
                    }
                }
            }
        }



        /// <summary>
        /// Разбирает отдельный оператор в зависимости от текущей лексемы.
        /// </summary>
        private void ParseStatement()
        {
            switch (_lexicalAnalyzer.CurrentLexem)
            {
                case Lexems.Name:
                    ParseAssignmentStatement();
                    break;
                case Lexems.If:
                    ParseIfStatement();
                    break;
                case Lexems.While:
                    ParseWhileStatement();
                    break;
                case Lexems.Type:
                    ParseVariableDeclaration();
                    break;
                case Lexems.Begin:
                    _lexicalAnalyzer.ParseNextLexem();
                    ParseStatementSequence();
                    CheckLexem(Lexems.End);
                    break;
                default:
                    Error($"Неожиданная лексема {_lexicalAnalyzer.CurrentLexem}");
                    _lexicalAnalyzer.ParseNextLexem();
                    break;
            }
        }

        /// <summary>
        /// Разбирает оператор if и соответствующие ветки then и else.
        /// </summary>
        private void ParseIfStatement()
        {
            _lexicalAnalyzer.ParseNextLexem(); // Переходим к следующей лексеме после if

            // Проверяем наличие открывающей скобки, если это часть вашей грамматики
            CheckLexem(Lexems.LeftPar);
            ParseExpression();
            CheckLexem(Lexems.RightPar);

            // Проверяем наличие then
            CheckLexem(Lexems.Then);

            // Разбираем последовательность операторов внутри блока then
            ParseStatementSequence();

            // Проверяем наличие else (если есть)
            if (_lexicalAnalyzer.CurrentLexem == Lexems.Else)
            {
                _lexicalAnalyzer.ParseNextLexem();
                // Разбираем последовательность операторов внутри блока else
                ParseStatementSequence();
            }

            // Ожидаем EndIf после всех веток
            CheckLexem(Lexems.EndIf);
        }


        /// <summary>
        /// Разбирает оператор while, включая проверку условия и тела цикла.
        /// </summary>
        private void ParseWhileStatement()
        {
            _lexicalAnalyzer.ParseNextLexem();
            CheckLexem(Lexems.LeftPar);
            ParseExpression();
            CheckLexem(Lexems.RightPar);
            CheckLexem(Lexems.Do);
            ParseStatement();
        }

        private void CheckLexem(Lexems expectedLexem)
        {
            if (_lexicalAnalyzer.CurrentLexem != expectedLexem)
            {
                Error($"Ожидается {expectedLexem}, но найдено {_lexicalAnalyzer.CurrentLexem}");
            }
            else
            {
                _lexicalAnalyzer.ParseNextLexem();
            }
        }

        /// <summary>
        /// Добавляет сообщение об ошибке с указанием текущего положения в исходном коде.
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        private void Error(string message)
        {
            string errorMessage = $"Ошибка в строке {_lexicalAnalyzer._reader.LineNumber}, позиция {_lexicalAnalyzer._reader.SymbolPositionInLine}: {message}";
            _errors.Add(errorMessage);
        }

        /// <summary>
        /// Проверяет совместимость типов двух операндов в операции.
        /// </summary>
        /// <param name="leftType">Тип левого операнда</param>
        /// <param name="rightType">Тип правого операнда</param>
        /// <param name="operation">Операция</param>
        private void CheckTypeCompatibility(tType leftType, tType rightType, string operation)
        {
            if (leftType != rightType)
            {
                Error($"Несовместимые типы в операции {operation}: {leftType} и {rightType}");
            }
        }

        /// <summary>
        /// Разбирает выражение с операциями сложения и вычитания.
        /// </summary>
        private tType ParseAdditionOrSubtraction()
        {
            tType type = ParseMultiplicationOrDivision();
            while (_lexicalAnalyzer.CurrentLexem == Lexems.Plus || _lexicalAnalyzer.CurrentLexem == Lexems.Minus)
            {
                var operation = _lexicalAnalyzer.CurrentLexem;
                _lexicalAnalyzer.ParseNextLexem();
                var rightType = ParseMultiplicationOrDivision();
                CheckTypeCompatibility(type, rightType, operation.ToString());
            }
            return type;
        }

        /// <summary>
        /// Разбирает выражение с операциями умножения и деления.
        /// </summary>
        private tType ParseMultiplicationOrDivision()
        {
            tType type = ParseSubexpression();
            while (_lexicalAnalyzer.CurrentLexem == Lexems.Multiplication || _lexicalAnalyzer.CurrentLexem == Lexems.Division)
            {
                var operation = _lexicalAnalyzer.CurrentLexem;
                _lexicalAnalyzer.ParseNextLexem();
                var rightType = ParseSubexpression();
                CheckTypeCompatibility(type, rightType, operation.ToString());
            }
            return type;
        }

        /// <summary>
        /// Разбирает подвыражение, такое как идентификатор, число или выражение в скобках.
        /// </summary>
        private tType ParseSubexpression()
        {
            if (_lexicalAnalyzer.CurrentLexem == Lexems.Name)
            {
                var identifier = _lexicalAnalyzer.GetNameTable().FindByName(_lexicalAnalyzer.CurrentName);
                if (identifier.Name != null && identifier.Category == tCat.Var)
                {
                    _lexicalAnalyzer.ParseNextLexem();
                    return identifier.Type;
                }
                Error($"Неопределенная переменная '{_lexicalAnalyzer.CurrentName}'");
            }
            else if (_lexicalAnalyzer.CurrentLexem == Lexems.Number)
            {
                _lexicalAnalyzer.ParseNextLexem();
                return tType.Int;
            }
            else if (_lexicalAnalyzer.CurrentLexem == Lexems.LeftPar)
            {
                _lexicalAnalyzer.ParseNextLexem();
                var type = ParseExpression();
                CheckLexem(Lexems.RightPar);
                return type;
            }
 
            Error($"Неожиданная лексема {_lexicalAnalyzer.CurrentLexem}");
            _lexicalAnalyzer.ParseNextLexem();
            return tType.None;
        }
        /// <summary>
        /// Разбирает выражение с операторами сравнения (>, <, >=, <=, ==, !=).
        /// </summary>
        private tType ParseComparison()
        {
            tType type = ParseAdditionOrSubtraction(); // Начинаем с обработки арифметических операций

            // Проверка на операторы сравнения
            if (_lexicalAnalyzer.CurrentLexem == Lexems.Greater ||
                _lexicalAnalyzer.CurrentLexem == Lexems.Less ||
                _lexicalAnalyzer.CurrentLexem == Lexems.GreaterOrEqual ||
                _lexicalAnalyzer.CurrentLexem == Lexems.LessOrEqual ||
                _lexicalAnalyzer.CurrentLexem == Lexems.Equal ||
                _lexicalAnalyzer.CurrentLexem == Lexems.NotEqual)
            {
                var comparisonOperator = _lexicalAnalyzer.CurrentLexem;
                _lexicalAnalyzer.ParseNextLexem();
                var rightType = ParseAdditionOrSubtraction(); // Разбираем правую часть выражения

                // Здесь можно добавить проверку совместимости типов для сравнения
                CheckTypeCompatibility(type, rightType, comparisonOperator.ToString());

                // Для операторов сравнения возвращаем тип bool
                return tType.Bool;
            }

            return type;
        }

        /// <summary>
        /// Разбирает выражение, начиная с операторов сравнения.
        /// </summary>
        private tType ParseExpression()
        {
            return ParseComparison();
        }

        /// <summary>
        /// Разбирает оператор присваивания, проверяет совместимость типов.
        /// </summary>
        private void ParseAssignmentStatement()
        {
            var identifier = _lexicalAnalyzer.GetNameTable().FindByName(_lexicalAnalyzer.CurrentName);
            if (identifier.Name == null)
            {
                Error($"Неопределенная переменная '{_lexicalAnalyzer.CurrentName}'");
                return;
            }

            _lexicalAnalyzer.ParseNextLexem();
            CheckLexem(Lexems.Assign);
            var expressionType = ParseExpression();
            CheckTypeCompatibility(identifier.Type, expressionType, "присваивания");
        }

        /// <summary>
        /// Разбирает объявление переменной и добавляет идентификаторы в таблицу имен с соответствующим типом.
        /// </summary>
        private void ParseVariableDeclaration()
        {
            if (_lexicalAnalyzer.CurrentLexem != Lexems.Type)
            {
                Error("Ожидался тип данных");
                return;
            }

            string dataType = _lexicalAnalyzer.CurrentName;
            tType variableType = DetermineTypeFromName(dataType); // Функция, определяющая тип по имени

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
                    _lexicalAnalyzer.GetNameTable().AddIdentifier(identifierName, tCat.Var, variableType);
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

            CheckLexem(Lexems.Semi);
        }

        /// <summary>
        /// Определяет тип данных на основе имени типа.
        /// </summary>
        private tType DetermineTypeFromName(string typeName)
        {
            switch (typeName.ToLower())
            {
                case "int":
                    return tType.Int;
                case "string":
                    return tType.String;
                case "bool":
                    return tType.Bool;
                case "float":
                    return tType.Float;
                case "double":
                    return tType.Double;
                case "char":
                    return tType.Char;
                default:
                    return tType.None;
            }
        }

        /// <summary>
        /// Запускает процесс синтаксического анализа и сообщает об обнаруженных ошибках.
        /// </summary>
        public void Compile()
        {
            _lexicalAnalyzer.ParseNextLexem();
            ParseVariableDeclaration();

            if (_lexicalAnalyzer.CurrentLexem == Lexems.Begin)
            {
                _lexicalAnalyzer.ParseNextLexem();
                ParseStatementSequence();
                CheckLexem(Lexems.End);
            }
            else
            {
                Error("Ожидалось начало блока кода (Begin)");
            }

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
