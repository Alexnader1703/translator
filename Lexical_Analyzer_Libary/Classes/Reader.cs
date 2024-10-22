using System;
using System.IO;

namespace Lexical_Analyzer_Libary.Classes
{
    public class Reader
    {
        private int lineNumber;
        private int symbolPositionInLine;
        private int currentSymbol;
        private TextReader textReader;

        public int LineNumber => lineNumber;
        public int SymbolPositionInLine => symbolPositionInLine;
        public int CurrentSymbol => currentSymbol;

        public void ReadNextSymbol()
        {
            currentSymbol = textReader.Read();
            if (currentSymbol == -1)
            {
                currentSymbol = char.MaxValue;
            }
            else if (currentSymbol == '\n')
            {
                lineNumber++;
                symbolPositionInLine = 0;
            }
            else if (currentSymbol == '\r' || currentSymbol == '\t')
            {
                ReadNextSymbol();
            }
            else
            {
                symbolPositionInLine++;
            }
        }

        public Reader(string filePath)
        {
            if (File.Exists(filePath))
            {
                Close();
                textReader = new StreamReader(filePath);
                lineNumber = 1;
                symbolPositionInLine = 0;
                ReadNextSymbol();
            }
            else
            {
                throw new FileNotFoundException("Файл не найден", filePath);
            }
        }

        public Reader(StringReader stringReader)
        {
            Close();
            textReader = stringReader;
            lineNumber = 1;
            symbolPositionInLine = 0;
            ReadNextSymbol();
        }

        public Reader(string inputString, bool isStringInput)
        {
            if (isStringInput)
            {
                textReader = new StringReader(inputString);
                lineNumber = 1;
                symbolPositionInLine = 0;
                ReadNextSymbol();
            }
            else
            {
                throw new ArgumentException("Invalid string input");
            }
        }

        public void Close()
        {
            if (textReader != null)
            {
                textReader.Close();
                textReader = null;
            }
        }

        public bool IsEndOfFile()
        {
            return currentSymbol == char.MaxValue;
        }
    }
}
