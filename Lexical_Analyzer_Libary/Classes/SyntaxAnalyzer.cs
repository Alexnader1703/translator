using System;
using System.Collections.Generic;

namespace Lexical_Analyzer_Libary.Classes
{
    public class SyntaxAnalyzer
    {
        private LexicalAnalyzer _lexicalAnalyzer;
        public List<string> _errors;

        // Поле для хранения текущей метки
        private string currentLabel = "";

        public SyntaxAnalyzer(LexicalAnalyzer lexicalAnalyzer)
        {
            _lexicalAnalyzer = lexicalAnalyzer;
            _errors = new List<string>();
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
                string jumpInstruction = "";
                switch (_lexicalAnalyzer.CurrentLexem)
                {
                    case Lexems.Equal:
                        jumpInstruction = "jne";
                        break;
                    case Lexems.NotEqual:
                        jumpInstruction = "je";
                        break;
                    case Lexems.Less:
                        jumpInstruction = "jge";
                        break;
                    case Lexems.Greater:
                        jumpInstruction = "jle";
                        break;
                    case Lexems.LessOrEqual:
                        jumpInstruction = "jg";
                        break;
                    case Lexems.GreaterOrEqual:
                        jumpInstruction = "jl";
                        break;
                }

                _lexicalAnalyzer.ParseNextLexem();
                tType rightType = ParseAdditionOrSubtraction();

                // Проверяем совместимость типов
                CheckTypeCompatibility(type, rightType, _lexicalAnalyzer.CurrentLexem.ToString());

                // Генерация кода для оператора сравнения
                CodeGenerator.AddInstruction("pop ax"); // Правый операнд
                CodeGenerator.AddInstruction("pop bx"); // Левый операнд
                CodeGenerator.AddInstruction("cmp bx, ax");
                CodeGenerator.AddInstruction($"{jumpInstruction} {currentLabel}");

                currentLabel = "";

                type = tType.Bool;
            }

            return type;
        }

        private void ParseStatementSequence()
        {
            while (true)
            {
                // Проверяем наличие конца блока
                if (_lexicalAnalyzer.CurrentLexem == Lexems.End ||
                    _lexicalAnalyzer.CurrentLexem == Lexems.EOF ||
                    _lexicalAnalyzer.CurrentLexem == Lexems.EndIf ||
                    _lexicalAnalyzer.CurrentLexem == Lexems.Else ||
                    _lexicalAnalyzer.CurrentLexem == Lexems.EndWhile ||
                    _lexicalAnalyzer.CurrentLexem == Lexems.ElseIf)
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
                         _lexicalAnalyzer.CurrentLexem != Lexems.Else &&
                         _lexicalAnalyzer.CurrentLexem != Lexems.EndWhile)
                {
                    Error("Ожидается точка с запятой");
                    // Пропуск до следующей точки с запятой или конца блока
                    while (_lexicalAnalyzer.CurrentLexem != Lexems.Semi &&
                           _lexicalAnalyzer.CurrentLexem != Lexems.End &&
                           _lexicalAnalyzer.CurrentLexem != Lexems.EOF &&
                           _lexicalAnalyzer.CurrentLexem != Lexems.EndIf &&
                           _lexicalAnalyzer.CurrentLexem != Lexems.Else &&
                           _lexicalAnalyzer.CurrentLexem != Lexems.EndWhile)
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
                    ParseWhileLoop();
                    break;
                case Lexems.Case:
                    ParseCaseStatement();
                    break;
                case Lexems.Type:
                    ParseVariableDeclaration();
                    break;
                case Lexems.Begin:
                    _lexicalAnalyzer.ParseNextLexem();
                    ParseStatementSequence();
                    CheckLexem(Lexems.End);
                    break;
                case Lexems.Print:
                    ParsePrintStatement();
                    break;
                default:
                    Error($"Неожиданная лексема {_lexicalAnalyzer.CurrentLexem}");
                    _lexicalAnalyzer.ParseNextLexem();
                    break;
            }
        }


        /// <summary>
        /// Разбирает оператор присваивания и генерирует соответствующий код.
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

            // Генерация кода для присваивания
            CodeGenerator.AddInstruction("pop ax");
            CodeGenerator.AddInstruction($"mov {identifier.Name}, ax");
        }

        /// <summary>
        /// Разбирает оператор вывода на печать и генерирует соответствующий код.
        /// </summary>
        private void ParsePrintStatement()
        {
            CheckLexem(Lexems.Print); // Проверяем лексему print

            if (_lexicalAnalyzer.CurrentLexem == Lexems.Name)
            {
                var identifier = _lexicalAnalyzer.GetNameTable().FindByName(_lexicalAnalyzer.CurrentName);
                if (identifier.Name != null)
                {
                    CodeGenerator.GeneratePrint(identifier.Name);
                    _lexicalAnalyzer.ParseNextLexem();
                }
                else
                {
                    Error($"Неопределенная переменная '{_lexicalAnalyzer.CurrentName}'");
                    _lexicalAnalyzer.ParseNextLexem();
                }
            }
            else
            {
                Error("Ожидался идентификатор после print");
            }
        }


        /// <summary>
        /// Разбирает выражение и генерирует соответствующий код для арифметических операций.
        /// </summary>
        private tType ParseExpression()
        {
            return ParseComparison();
        }

        /// <summary>
        /// Разбирает операции сложения и вычитания.
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

                // Генерация кода для операций сложения и вычитания
                switch (operation)
                {
                    case Lexems.Plus:
                        CodeGenerator.GenerateAddition();
                        break;
                    case Lexems.Minus:
                        CodeGenerator.GenerateSubtraction();
                        break;
                }
            }

            return type;
        }

        /// <summary>
        /// Разбирает операции умножения и деления.
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

                // Генерация кода для операций умножения и деления
                switch (operation)
                {
                    case Lexems.Multiplication:
                        CodeGenerator.GenerateMultiplication();
                        break;
                    case Lexems.Division:
                        CodeGenerator.GenerateDivision();
                        break;
                }
            }

            return type;
        }

        /// <summary>
        /// Разбирает подвыражение (идентификатор, число или выражение в скобках) и генерирует соответствующий код.
        /// </summary>
        private tType ParseSubexpression()
        {
            if (_lexicalAnalyzer.CurrentLexem == Lexems.Name)
            {
                var identifier = _lexicalAnalyzer.GetNameTable().FindByName(_lexicalAnalyzer.CurrentName);
                if (identifier.Name != null && identifier.Category == tCat.Var)
                {
                    // Генерация кода для загрузки переменной
                    CodeGenerator.AddInstruction($"mov ax, {identifier.Name}");
                    CodeGenerator.AddInstruction("push ax");

                    _lexicalAnalyzer.ParseNextLexem();
                    return identifier.Type;
                }
                Error($"Неопределенная переменная '{_lexicalAnalyzer.CurrentName}'");
                _lexicalAnalyzer.ParseNextLexem();
            }
            else if (_lexicalAnalyzer.CurrentLexem == Lexems.Number)
            {
                // Генерация кода для загрузки числа
                CodeGenerator.AddInstruction($"mov ax, {_lexicalAnalyzer.CurrentNumber}");
                CodeGenerator.AddInstruction("push ax");

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
            // Добавляем поддержку булевых литералов true и false
            else if (_lexicalAnalyzer.CurrentLexem == Lexems.True)
            {
                CodeGenerator.AddInstruction("mov ax, 1");
                CodeGenerator.AddInstruction("push ax");

                _lexicalAnalyzer.ParseNextLexem();
                return tType.Bool;
            }
            else if (_lexicalAnalyzer.CurrentLexem == Lexems.False)
            {
                CodeGenerator.AddInstruction("mov ax, 0");
                CodeGenerator.AddInstruction("push ax");

                _lexicalAnalyzer.ParseNextLexem();
                return tType.Bool;
            }

            Error($"Неожиданная лексема {_lexicalAnalyzer.CurrentLexem}");
            _lexicalAnalyzer.ParseNextLexem();
            return tType.None;
        }

        /// <summary>
        /// Разбирает оператор if и соответствующие ветки then и else.
        /// </summary>
        private void ParseIfStatement()
        {
            CheckLexem(Lexems.If);

            // Генерация меток
            string lowerLabel = CodeGenerator.GenerateLabel();
            currentLabel = lowerLabel;

            string exitLabel = CodeGenerator.GenerateLabel();

            CheckLexem(Lexems.LeftPar);
            ParseExpression();
            CheckLexem(Lexems.RightPar);

            CheckLexem(Lexems.Then);

            ParseStatementSequence();

            CodeGenerator.AddInstruction($"jmp {exitLabel}");

            CodeGenerator.AddLabel(lowerLabel);

            while (_lexicalAnalyzer.CurrentLexem == Lexems.ElseIf)
            {
                _lexicalAnalyzer.ParseNextLexem();

                lowerLabel = CodeGenerator.GenerateLabel();
                currentLabel = lowerLabel;

                CheckLexem(Lexems.LeftPar);
                ParseExpression();
                CheckLexem(Lexems.RightPar);

                CheckLexem(Lexems.Then);

                ParseStatementSequence();

                CodeGenerator.AddInstruction($"jmp {exitLabel}");

                CodeGenerator.AddLabel(lowerLabel);
            }

            if (_lexicalAnalyzer.CurrentLexem == Lexems.Else)
            {
                _lexicalAnalyzer.ParseNextLexem();
                ParseStatementSequence();
            }

            CheckLexem(Lexems.EndIf);

            CodeGenerator.AddLabel(exitLabel);
        }

        /// <summary>
        /// Разбирает оператор while, включая проверку условия и тела цикла.
        /// </summary>
        private void ParseWhileLoop()
        {
            CheckLexem(Lexems.While);

            string upperLabel = CodeGenerator.GenerateLabel();
            string lowerLabel = CodeGenerator.GenerateLabel();
            currentLabel = lowerLabel;

            CodeGenerator.AddLabel(upperLabel);

            CheckLexem(Lexems.LeftPar);
            ParseExpression();
            CheckLexem(Lexems.RightPar);

            // Генерация кода для условия цикла будет в ParseComparison() через currentLabel

            ParseStatementSequence();

            CodeGenerator.AddInstruction($"jmp {upperLabel}");
            CodeGenerator.AddLabel(lowerLabel);

            CheckLexem(Lexems.EndWhile);
        }

        /// <summary>
        /// Разбирает объявление переменных и добавляет их в таблицу имен.
        /// </summary>
        private void ParseVariableDeclaration()
        {
            if (_lexicalAnalyzer.CurrentLexem != Lexems.Type)
            {
                Error("Ожидался тип данных");
                return;
            }

            string dataType = _lexicalAnalyzer.CurrentName;
            tType variableType = DetermineTypeFromName(dataType);

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

                    // Добавляем генерацию кода для инициализации булевых переменных
                    if (variableType == tType.Bool)
                    {
                        CodeGenerator.AddInstruction($"mov {identifierName}, 0"); // Инициализация как false
                    }
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
        /// Определяет тип переменной на основе имени типа.
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
        /// Проверяет, соответствует ли текущая лексема ожидаемой.
        /// </summary>
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
        private void Error(string message)
        {
            string errorMessage = $"Ошибка в строке {_lexicalAnalyzer._reader.LineNumber}, позиция {_lexicalAnalyzer._reader.SymbolPositionInLine}: {message}";
            _errors.Add(errorMessage);
        }

        /// <summary>
        /// Проверяет совместимость типов в операции.
        /// </summary>
        private void CheckTypeCompatibility(tType leftType, tType rightType, string operation)
        {
            if (leftType != rightType)
            {
                Error($"Несовместимые типы в операции {operation}: {leftType} и {rightType}");
            }
        }

        /// <summary>
        /// Запускает процесс компиляции и вызывает необходимые методы генератора кода.
        /// </summary>
        public void Compile()
        {
            // Объявление сегмента данных
            CodeGenerator.DeclareDataSegment();

            _lexicalAnalyzer.ParseNextLexem();

            // Разрешаем несколько объявлений переменных перед Begin
            while (_lexicalAnalyzer.CurrentLexem == Lexems.Type)
            {
                ParseVariableDeclaration();
            }

            // Объявление переменных
            CodeGenerator.DeclareVariables(_lexicalAnalyzer.GetNameTable());

            // Объявление сегментов стека и кода
            CodeGenerator.DeclareStackAndCodeSegments();

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

            // Остальной код остается без изменений
            CodeGenerator.DeclareEndOfMainProcedure();
            CodeGenerator.DeclarePrintProcedure();
            CodeGenerator.DeclareEndOfCode();

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
        /// <summary>
        /// Разбирает конструкцию CASE и генерирует соответствующий код.
        /// </summary>
        private void ParseCaseStatement()
        {
            CheckLexem(Lexems.Case);

            if (_lexicalAnalyzer.CurrentLexem != Lexems.Name)
            {
                Error("Ожидался идентификатор после CASE");
                return;
            }

            string caseVariable = _lexicalAnalyzer.CurrentName;
            var identifier = _lexicalAnalyzer.GetNameTable().FindByName(caseVariable);
            if (identifier.Name == null || identifier.Type != tType.Int)
            {
                Error($"Переменная '{caseVariable}' не определена или не является целочисленной");
                return;
            }

            _lexicalAnalyzer.ParseNextLexem();
            CheckLexem(Lexems.Of);


            var cases = new List<(int value, string label, List<string> instructions)>();

            while (_lexicalAnalyzer.CurrentLexem == Lexems.Number)
            {
                int value = _lexicalAnalyzer.CurrentNumber;
                string label = CodeGenerator.GenerateLabel();
                var branchInstructions = new List<string>();

                _lexicalAnalyzer.ParseNextLexem();
                CheckLexem(Lexems.Colon);

                // Сохраняем все инструкции для этой ветки
                while (_lexicalAnalyzer.CurrentLexem != Lexems.Number &&
                       _lexicalAnalyzer.CurrentLexem != Lexems.EndCase)
                {
                    var currentInstructions = ParseStatementCase();
                    branchInstructions.AddRange(currentInstructions);
                }

                cases.Add((value, label, branchInstructions));
            }


            // 1. Загрузка переменной
            CodeGenerator.AddInstruction($"mov ax, {caseVariable}");

            string endLabel = CodeGenerator.GenerateLabel();

            // 2. Генерация всех сравнений
            foreach (var (value, label, _) in cases)
            {
                CodeGenerator.AddInstruction($"cmp ax, {value}");
                CodeGenerator.AddInstruction($"je {label}");
            }

            // 3. Переход на конец если ни одно условие не подошло
            CodeGenerator.AddInstruction($"jmp {endLabel}");

            // 4. Генерация кода для всех веток
            foreach (var (_, label, instructions) in cases)
            {
                CodeGenerator.AddLabel(label);
                foreach (var instruction in instructions)
                {
                    CodeGenerator.AddInstruction(instruction);
                }
                CodeGenerator.AddInstruction($"jmp {endLabel}");
            }

            CheckLexem(Lexems.EndCase);
            CodeGenerator.AddLabel(endLabel);
        }


        private List<string> ParseStatementCase()
        {
            var instructions = new List<string>();

            switch (_lexicalAnalyzer.CurrentLexem)
            {
                case Lexems.Name:
                    string varName = _lexicalAnalyzer.CurrentName;
                    _lexicalAnalyzer.ParseNextLexem();

                    if (_lexicalAnalyzer.CurrentLexem == Lexems.Assign)
                    {
                        _lexicalAnalyzer.ParseNextLexem();
                        if (_lexicalAnalyzer.CurrentLexem == Lexems.Number)
                        {
                            instructions.Add($"mov {varName}, {_lexicalAnalyzer.CurrentNumber}");
                            _lexicalAnalyzer.ParseNextLexem();
                        }
                    }
                    break;

                case Lexems.Semi:
                    _lexicalAnalyzer.ParseNextLexem();
                    break;


                default:
                    _lexicalAnalyzer.ParseNextLexem();
                    break;
            }

            return instructions;
        }

      


    }
}
