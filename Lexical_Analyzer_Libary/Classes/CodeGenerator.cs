using System;
using System.Collections.Generic;

namespace Lexical_Analyzer_Libary.Classes
{
    /// <summary>
    /// Генератор кода
    /// </summary>
    public class CodeGenerator
    {
        // Список для хранения ассемблерного кода
        private static List<string> code = new List<string>();

        // Счетчик для генерации уникальных меток
        private static int labelCounter = 0;

        /// <summary>
        /// Метод для добавления инструкции в код
        /// </summary>
        public static void AddInstruction(string instruction)
        {
            code.Add(instruction);
        }

        /// <summary>
        /// Метод для добавления метки в код
        /// </summary>
        public static void AddLabel(string label)
        {
            code.Add($"{label}:");
        }

        /// <summary>
        /// Объявление сегмента данных
        /// </summary>
        public static void DeclareDataSegment()
        {
            AddInstruction("data segment");
        }

        /// <summary>
        /// Объявление переменных из таблицы имен
        /// </summary>
        public static void DeclareVariables(NameTable nameTable)
        {
            var identifiers = nameTable.GetIdentifiers();
            foreach (var identifier in identifiers)
            {
                // Опционально: можно не инициализировать переменные или инициализировать корректно
                AddInstruction($"{identifier.Name} dw ?");
            }
            // Добавление буферов для печати
            AddInstruction("PRINT_BUF DB ' ' DUP(10)");
            AddInstruction("BUFEND    DB '$'");
            AddInstruction("data ends");
        }

        /// <summary>
        /// Объявление сегментов стека и кода
        /// </summary>
        public static void DeclareStackAndCodeSegments()
        {
            AddInstruction("stk segment stack");
            AddInstruction("db 256 dup ('?')");
            AddInstruction("stk ends");

            AddInstruction("code segment");
            AddInstruction("assume cs:code, ds:data, ss:stk");
            AddInstruction("main proc");

            AddInstruction("mov ax, data");
            AddInstruction("mov ds, ax");
        }

        /// <summary>
        /// Объявление процедуры печати
        /// </summary>
        public static void DeclarePrintProcedure()
        {
            AddInstruction("PRINT PROC NEAR");
            AddInstruction("MOV   CX, 10");
            AddInstruction("MOV   DI, BUFEND - PRINT_BUF");
            AddInstruction("PRINT_LOOP:");
            AddInstruction("MOV   DX, 0");
            AddInstruction("DIV   CX");
            AddInstruction("ADD   DL, '0'");
            AddInstruction("MOV   [PRINT_BUF + DI - 1], DL");
            AddInstruction("DEC   DI");
            AddInstruction("CMP   AL, 0");
            AddInstruction("JNE   PRINT_LOOP");
            AddInstruction("LEA   DX, PRINT_BUF");
            AddInstruction("ADD   DX, DI");
            AddInstruction("MOV   AH, 09H");
            AddInstruction("INT   21H");
            AddInstruction("RET");
            AddInstruction("PRINT ENDP");
        }

        /// <summary>
        /// Завершение основной процедуры
        /// </summary>
        public static void DeclareEndOfMainProcedure()
        {

            AddInstruction("mov ax, 4c00h");
            AddInstruction("int 21h");
            AddInstruction("main endp");
        }

        /// <summary>
        /// Завершение сегмента кода
        /// </summary>
        public static void DeclareEndOfCode()
        {
            AddInstruction("code ends");
            AddInstruction("end main");
        }

        // Методы для генерации кода арифметических операций
        public static void GenerateAddition()
        {
            AddInstruction("pop bx");
            AddInstruction("pop ax");
            AddInstruction("add ax, bx");
            AddInstruction("push ax");
        }

        public static void GenerateSubtraction()
        {
            AddInstruction("pop bx");
            AddInstruction("pop ax");
            AddInstruction("sub ax, bx");
            AddInstruction("push ax");
        }

        public static void GenerateMultiplication()
        {
            AddInstruction("pop bx");
            AddInstruction("pop ax");
            AddInstruction("mul bx");
            AddInstruction("push ax");
        }

        public static void GenerateDivision()
        {
            AddInstruction("pop bx");
            AddInstruction("pop ax");
            AddInstruction("cwd");
            AddInstruction("div bx");
            AddInstruction("push ax");
        }

        /// <summary>
        /// Метод для генерации кода вывода переменной на печать
        /// </summary>
        public static void GeneratePrint(string variableName)
        {
            AddInstruction($"mov ax, {variableName}");
            AddInstruction("CALL PRINT");
        }

        /// <summary>
        /// Метод для генерации уникальной метки
        /// </summary>
        public static string GenerateLabel()
        {
            return $"Label{labelCounter++}";
        }

        /// <summary>
        /// Метод для получения сгенерированного кода
        /// </summary>
        public static string[] GetGeneratedCode()
        {
            return code.ToArray();
        }

        /// <summary>
        /// Метод для очистки всего сгенерированного кода
        /// </summary>
        public static void ClearCode()
        {
            code.Clear();
            labelCounter = 0; // Сбрасываем счетчик меток для корректной генерации новых меток
        }

    }
}
