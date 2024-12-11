using System;
using System.Collections.Generic;

namespace Lexical_Analyzer_Libary.Classes
{
    public class SyntaxAnalyzer
    {
        private LexicalAnalyzer _lexicalAnalyzer;
        public List<string> _errors;

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
            tType type = ParseAdditionOrSubtraction();

            if (_lexicalAnalyzer.CurrentLexem == Lexems.Greater ||
                _lexicalAnalyzer.CurrentLexem == Lexems.Less ||
                _lexicalAnalyzer.CurrentLexem == Lexems.GreaterOrEqual ||
                _lexicalAnalyzer.CurrentLexem == Lexems.LessOrEqual ||
                _lexicalAnalyzer.CurrentLexem == Lexems.Equal ||
                _lexicalAnalyzer.CurrentLexem == Lexems.NotEqual)
            {
                string jumpInstruction = "";
                string falseLabel = CodeGenerator.GenerateLabel();
                string endLabel = CodeGenerator.GenerateLabel();

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
                CodeGenerator.AddInstruction($"{jumpInstruction} {falseLabel}");

                // Если условие истинно
                CodeGenerator.AddInstruction("mov ax, 1");
                CodeGenerator.AddInstruction("jmp " + endLabel);

                // Если условие ложно
                CodeGenerator.AddLabel(falseLabel);
                CodeGenerator.AddInstruction("mov ax, 0");

                CodeGenerator.AddLabel(endLabel);
                CodeGenerator.AddInstruction("push ax");

                type = tType.Bool;
            }

            return type;
        }
        private void ParseStatementSequence()
        {
            bool foundEnd = false;
            int maxStatements = 1000; // Защита от бесконечного цикла
            int statementCount = 0;

            while (!foundEnd && statementCount < maxStatements)
            {
                statementCount++;

                // Сначала проверяем EOF
                if (_lexicalAnalyzer.CurrentLexem == Lexems.EOF)
                {
                    Error("Неожиданный конец файла. Ожидалось 'end'");
                    return; // Немедленно прекращаем разбор
                }

                // Затем проверяем завершающие токены
                if (_lexicalAnalyzer.CurrentLexem == Lexems.End ||
                    _lexicalAnalyzer.CurrentLexem == Lexems.EndIf ||
                    _lexicalAnalyzer.CurrentLexem == Lexems.Else ||
                    _lexicalAnalyzer.CurrentLexem == Lexems.EndWhile ||
                    _lexicalAnalyzer.CurrentLexem == Lexems.ElseIf)
                {
                    foundEnd = true;
                    break;
                }

                // Разбор очередного оператора
                try
                {
                    ParseStatement();
                }
                catch (Exception ex)
                {
                    Error($"Ошибка при разборе оператора: {ex.Message}");
                    SkipToNextStatement();
                    continue;
                }

                // Проверка точки с запятой
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
                    SkipToNextStatement();
                }
            }

            // Проверка на превышение максимального количества операторов
            if (statementCount >= maxStatements)
            {
                Error("Превышено максимальное количество операторов в блоке. Возможно, пропущено 'end'");
                return;
            }

            // Если мы не нашли конец блока
            if (!foundEnd)
            {
                Error("Ожидалось 'end' в конце блока");
            }
        }

        private void SkipToNextStatement()
        {
            int maxSkips = 100; // Защита от зацикливания
            int skipCount = 0;

            while (skipCount < maxSkips &&
                   _lexicalAnalyzer.CurrentLexem != Lexems.Semi &&
                   _lexicalAnalyzer.CurrentLexem != Lexems.End &&
                   _lexicalAnalyzer.CurrentLexem != Lexems.EOF &&
                   _lexicalAnalyzer.CurrentLexem != Lexems.EndIf &&
                   _lexicalAnalyzer.CurrentLexem != Lexems.Else &&
                   _lexicalAnalyzer.CurrentLexem != Lexems.EndWhile)
            {
                _lexicalAnalyzer.ParseNextLexem();
                skipCount++;
            }

            if (skipCount >= maxSkips)
            {
                Error("Не удалось найти конец оператора");
                return;
            }

            if (_lexicalAnalyzer.CurrentLexem == Lexems.Semi)
            {
                _lexicalAnalyzer.ParseNextLexem();
            }
        }
        /// <summary>
        /// Разбирает отдельный оператор в зависимости от текущей лексемы.
        /// </summary>
        private void ParseStatement()
        {
            switch (_lexicalAnalyzer.CurrentLexem)
            {
                case Lexems.Var: // Новый кейс для 'var'
                    ParseVariableDeclaration();
                    break;
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
            return ParseLogicalOr();
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
            string falseLabel = CodeGenerator.GenerateLabel();  // метка для false-ветки
            string endLabel = CodeGenerator.GenerateLabel();    // метка конца if

            CheckLexem(Lexems.LeftPar);
            ParseExpression();  // результат условия будет на вершине стека
            CheckLexem(Lexems.RightPar);

            // После вычисления условия, проверяем его и делаем переход если false
            CodeGenerator.AddInstruction("pop ax");
            CodeGenerator.AddInstruction("cmp ax, 0");
            CodeGenerator.AddInstruction($"je {falseLabel}");

            CheckLexem(Lexems.Then);

            // Код для then-ветки
            ParseStatementSequence();

            // После then-ветки переходим в конец if
            CodeGenerator.AddInstruction($"jmp {endLabel}");

            // Метка для false-ветки
            CodeGenerator.AddLabel(falseLabel);

            // Обработка elseif веток
            while (_lexicalAnalyzer.CurrentLexem == Lexems.ElseIf)
            {
                _lexicalAnalyzer.ParseNextLexem();
                string nextFalseLabel = CodeGenerator.GenerateLabel();

                CheckLexem(Lexems.LeftPar);
                ParseExpression();
                CheckLexem(Lexems.RightPar);

                // Проверяем условие elseif
                CodeGenerator.AddInstruction("pop ax");
                CodeGenerator.AddInstruction("cmp ax, 0");
                CodeGenerator.AddInstruction($"je {nextFalseLabel}");

                CheckLexem(Lexems.Then);
                ParseStatementSequence();

                // После успешной ветки переходим в конец
                CodeGenerator.AddInstruction($"jmp {endLabel}");

                // Метка для следующей ветки
                CodeGenerator.AddLabel(nextFalseLabel);
                falseLabel = nextFalseLabel;
            }

            // Обработка else
            if (_lexicalAnalyzer.CurrentLexem == Lexems.Else)
            {
                _lexicalAnalyzer.ParseNextLexem();
                ParseStatementSequence();
            }

            CheckLexem(Lexems.EndIf);

            // Метка конца всего if
            CodeGenerator.AddLabel(endLabel);
        }

        /// <summary>
        /// Разбирает оператор while, включая проверку условия и тела цикла.
        /// </summary>
        private void ParseWhileLoop()
        {
            CheckLexem(Lexems.While);

            string startLabel = CodeGenerator.GenerateLabel();  // Метка начала цикла
            string endLabel = CodeGenerator.GenerateLabel();    // Метка конца цикла

            // Метка начала цикла
            CodeGenerator.AddLabel(startLabel);

            CheckLexem(Lexems.LeftPar);
            ParseExpression();  // Разбор условия
            CheckLexem(Lexems.RightPar);

            // Проверка условия и выход из цикла если оно ложно
            CodeGenerator.AddInstruction("pop ax");
            CodeGenerator.AddInstruction("cmp ax, 0");
            CodeGenerator.AddInstruction($"je {endLabel}");

            // Разбор тела цикла
            ParseStatementSequence();

            // Переход обратно к началу цикла
            CodeGenerator.AddInstruction($"jmp {startLabel}");

            // Метка конца цикла
            CodeGenerator.AddLabel(endLabel);

            CheckLexem(Lexems.EndWhile);
        }

        /// <summary>
        /// Разбирает объявление переменных и добавляет их в таблицу имен.
        /// </summary>
        private void ParseVariableDeclaration()
        {
            // Проверяем наличие 'var'
            CheckLexem(Lexems.Var); // Ожидается Lexems.Var, уже обрабатывается в ParseStatement

            // Проверяем наличие двоеточия после 'var'
            CheckLexem(Lexems.Colon, "Ожидался символ ':' после 'var'");

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
                case "boolean":
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
        private void CheckLexem(Lexems expectedLexem, string errorMessage = null)
        {
            if (_lexicalAnalyzer.CurrentLexem != expectedLexem)
            {
                if (errorMessage == null)
                {
                    Error($"Ожидается {expectedLexem}, но найдено {_lexicalAnalyzer.CurrentLexem}");
                }
                else
                {
                    Error(errorMessage);
                }
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
            if (operation == "логического И" || operation == "логического ИЛИ")
            {
                if (leftType != tType.Bool || rightType != tType.Bool)
                {
                    Error($"Операции {operation} применимы только к типу 'bool'");
                }
                return;
            }

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

            // Разрешаем несколько объявлений переменных с 'var:'
            while (_lexicalAnalyzer.CurrentLexem == Lexems.Var)
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
        private tType ParseLogicalOr()
        {
            tType type = ParseLogicalAnd();

            while (_lexicalAnalyzer.CurrentLexem == Lexems.Or)
            {
                string exitLabel = CodeGenerator.GenerateLabel();

                _lexicalAnalyzer.ParseNextLexem();
                tType rightType = ParseLogicalAnd();
                CheckTypeCompatibility(type, rightType, "логического ИЛИ");

                // Генерация кода для операции OR
                CodeGenerator.AddInstruction("pop ax"); // Правый операнд
                CodeGenerator.AddInstruction("pop bx"); // Левый операнд
                CodeGenerator.AddInstruction("or ax, bx");
                CodeGenerator.AddInstruction("push ax");
                CodeGenerator.AddInstruction("cmp ax, 0");
                CodeGenerator.AddInstruction($"jnz {exitLabel}"); // Если результат не ноль, пропускаем jump
                CodeGenerator.AddInstruction($"jmp {currentLabel}"); // Если результат ноль, переходим на метку условия
                CodeGenerator.AddLabel(exitLabel);

                type = tType.Bool;
            }
            return type;
        }

        private tType ParseLogicalAnd()
        {
            tType type = ParseComparison();

            while (_lexicalAnalyzer.CurrentLexem == Lexems.And)
            {
                _lexicalAnalyzer.ParseNextLexem();

                // Сохраняем текущую метку, так как ParseComparison может её изменить
                string savedLabel = currentLabel;

                tType rightType = ParseComparison();
                CheckTypeCompatibility(type, rightType, "логического И");

                // Генерация кода для операции AND
                CodeGenerator.AddInstruction("pop ax"); // Правый операнд
                CodeGenerator.AddInstruction("pop bx"); // Левый операнд
                CodeGenerator.AddInstruction("and ax, bx");
                CodeGenerator.AddInstruction("push ax");
                CodeGenerator.AddInstruction("cmp ax, 0");
                CodeGenerator.AddInstruction($"jz {savedLabel}"); // Если результат ноль, переходим на метку условия

                type = tType.Bool;
            }
            return type;
        }



    }
}
