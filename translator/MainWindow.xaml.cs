using Microsoft.Win32;
using System;
using System.Diagnostics; // Для работы с процессами
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Lexical_Analyzer_Libary.Classes;
using System.Linq;
using System.Collections.Generic;

namespace translator
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Process process; // Поле для управления процессом DOSBox

        // Ресурсные кисти
        private Brush primaryBrush;
        private Brush secondaryBrush;
        private Brush accentBrush;
        private Brush foregroundBrush;
        private Brush errorBrush;

        public MainWindow()
        {
            InitializeComponent();

            // Инициализация ресурсных кистей
            primaryBrush = (Brush)FindResource("PrimaryBrush");
            secondaryBrush = (Brush)FindResource("SecondaryBrush");
            accentBrush = (Brush)FindResource("AccentBrush");
            foregroundBrush = (Brush)FindResource("ForegroundBrush");

            // Определение кисти для ошибок (можно добавить в ресурсы XAML)
            errorBrush = new SolidColorBrush(Colors.Red);
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            // Код для открытия файла
            OpenFileDialog openFileDialog = new OpenFileDialog();

            if (openFileDialog.ShowDialog() == true)
            {
                Reader _reader = new Reader(openFileDialog.FileName);
                try
                {
                    StringBuilder fileContent = new StringBuilder();
                    while (!_reader.IsEndOfFile())
                    {
                        fileContent.Append((char)_reader.CurrentSymbol); // просто добавляем символ
                        _reader.ReadNextSymbol();
                    }

                    SourceTextBox.Document.Blocks.Clear();
                    SourceTextBox.Document.Blocks.Add(new Paragraph(new Run(fileContent.ToString())));

                    // Очищаем MessageTextBox и добавляем сообщение об успехе
                    MessageTextBox.Document.Blocks.Clear();
                    AddMessage("Файл успешно загружен и прочитан", MessageType.Success);
                }
                catch (Exception ex)
                {
                    MessageTextBox.Document.Blocks.Clear();
                    AddMessage($"Ошибка при чтении файла: {ex.Message}", MessageType.Error);
                }
                finally
                {
                    _reader.Close();
                }
            }
        }

        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            // Код для сохранения файла
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == true)
            {
                TextRange textRange = new TextRange(ResultTextBox.Document.ContentStart, ResultTextBox.Document.ContentEnd);
                File.WriteAllText(saveFileDialog.FileName, textRange.Text);
            }
        }

        private void Compile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Очищаем файл Code.asm
                string tasmPath = Environment.CurrentDirectory + "\\TASM";
                string asmFilePath = tasmPath + "\\Code.asm";
                if (File.Exists(asmFilePath))
                {
                    File.WriteAllText(asmFilePath, string.Empty);
                }

                // Очищаем текстбоксы
                ResultTextBox.Document.Blocks.Clear();
                MessageTextBox.Document.Blocks.Clear();
                CodeGenerator.ClearCode();

                // Чтение исходного кода из текстбокса
                TextRange sourceTextRange = new TextRange(SourceTextBox.Document.ContentStart, SourceTextBox.Document.ContentEnd);
                string sourceCode = sourceTextRange.Text;

                // Создаем экземпляр лексического анализатора
                LexicalAnalyzer lexicalAnalyzer = new LexicalAnalyzer(sourceCode, true);

                // Создаем экземпляр синтаксического анализатора
                SyntaxAnalyzer syntaxAnalyzer = new SyntaxAnalyzer(lexicalAnalyzer);

                // Запускаем компиляцию
                syntaxAnalyzer.Compile();

                if (syntaxAnalyzer._errors.Count > 0)
                {
                    StringBuilder errorsOutput = new StringBuilder("Ошибки синтаксического анализа:\n");
                    foreach (var error in syntaxAnalyzer._errors)
                    {
                        errorsOutput.AppendLine(error);
                    }
                    AddMessage(errorsOutput.ToString(), MessageType.Error);
                }
                else
                {
                    AddMessage("Компиляция выполнена успешно", MessageType.Success);

                    // Создаем директорию TASM, если её нет
                    if (!Directory.Exists(tasmPath))
                    {
                        Directory.CreateDirectory(tasmPath);
                    }

                    // Сохраняем ассемблерный код в файл
                    string[] generatedCode = CodeGenerator.GetGeneratedCode();
                    File.WriteAllLines(asmFilePath, generatedCode);

                    // Выводим сгенерированный код в ResultTextBox
                    StringBuilder codeOutput = new StringBuilder("Сгенерированный ассемблерный код:\n");
                    foreach (var line in generatedCode)
                    {
                        codeOutput.AppendLine(line);
                    }
                    AddCodeToResultTextBox(codeOutput.ToString());

                    // Создаем файл Run.bat
                    string runBatContent = @"MASM.exe Code.asm,,,;
LINK.exe Code.obj,,,;
Code.exe";
                    File.WriteAllText(tasmPath + "\\Run.bat", runBatContent);
                }
            }
            catch (Exception ex)
            {
                MessageTextBox.Document.Blocks.Clear();
                AddMessage($"Ошибка при компиляции: {ex.Message}", MessageType.Error);
            }
        }

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, есть ли ошибки компиляции
            if (HasCompilationErrors())
            {
                MessageBox.Show("Обнаружены ошибки компиляции. Исполнение программы невозможно.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Получаем текущую директорию и путь к папке TASM
                string tasmPath = Environment.CurrentDirectory + "\\TASM";

                // Проверяем существование необходимых файлов
                if (!File.Exists(tasmPath + "\\DOSBox.exe"))
                {
                    MessageBox.Show("Не найден DOSBox.exe в папке TASM.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!File.Exists(tasmPath + "\\Run.bat"))
                {
                    MessageBox.Show("Не найден Run.bat в папке TASM.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Создаем процесс для запуска DOSBox с нашим пакетным файлом
                process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = tasmPath + "\\DOSBox.exe",
                    Arguments = $"\"{tasmPath}\\Run.bat\" -noconsole",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = tasmPath
                };
                process.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при запуске программы: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Compile_Click(sender, e); // Вызываем метод компиляции при нажатии Enter
            }
        }

        // Перечисление для определения типа сообщения
        private enum MessageType
        {
            Success,
            Error,
            Info
        }

        // Метод для добавления сообщений в MessageTextBox с соответствующим стилем
        private void AddMessage(string message, MessageType messageType)
        {
            Paragraph paragraph = new Paragraph(new Run(message))
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = GetBrushForMessageType(messageType)
            };
            MessageTextBox.Document.Blocks.Add(paragraph);
        }

        // Метод для определения кисти на основе типа сообщения
        private Brush GetBrushForMessageType(MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.Success:
                    return accentBrush; // Можно использовать accentBrush или любую другую кисть
                case MessageType.Error:
                    return errorBrush;
                case MessageType.Info:
                default:
                    return foregroundBrush;
            }
        }

        // Метод для добавления кода в ResultTextBox с соответствующим стилем
        private void AddCodeToResultTextBox(string code)
        {
            Paragraph codeParagraph = new Paragraph(new Run(code))
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                FontWeight = FontWeights.Normal,
                Foreground = foregroundBrush // Используем foregroundBrush из ресурсов
            };
            ResultTextBox.Document.Blocks.Add(codeParagraph);
        }

        // Метод для проверки наличия ошибок компиляции
        private bool HasCompilationErrors()
        {
            foreach (var block in MessageTextBox.Document.Blocks)
            {
                if (block is Paragraph paragraph)
                {
                    if (paragraph.Inlines.FirstInline is Run run && run.Foreground == errorBrush)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private List<TokenInfo> GetTokens(LexicalAnalyzer lexer)
        {
            List<TokenInfo> tokens = new List<TokenInfo>();

            TokenInfo token;
            while (true)
            {
                int startIndex = lexer._reader.GlobalPosition;

                lexer.ParseNextLexem();

                int endIndex = lexer._reader.GlobalPosition;

                if (lexer.CurrentLexem == Lexems.EOF)
                    break;

                tokens.Add(new TokenInfo
                {
                    Lexem = lexer.CurrentLexem,
                    Value = lexer.CurrentLexem == Lexems.Name || lexer.CurrentLexem == Lexems.Type ? lexer.CurrentName :
                            lexer.CurrentLexem == Lexems.Number ? lexer.CurrentNumber.ToString() : null,
                    StartIndex = startIndex,
                    EndIndex = endIndex
                });

                if (lexer.CurrentLexem == Lexems.Error)
                    break;
            }

            return tokens;
        }
        private TextPointer GetTextPointerAtOffset(TextPointer start, int offset)
        {
            TextPointer navigator = start;
            int count = 0;

            while (navigator != null)
            {
                if (navigator.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string textRun = navigator.GetTextInRun(LogicalDirection.Forward);
                    int runLength = textRun.Length;

                    if (count + runLength >= offset)
                    {
                        return navigator.GetPositionAtOffset(offset - count);
                    }

                    count += runLength;
                    navigator = navigator.GetPositionAtOffset(runLength);
                }
                else
                {
                    navigator = navigator.GetNextContextPosition(LogicalDirection.Forward);
                }
            }

            return start;
        }
        private void SourceTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Получаем весь текст из RichTextBox
            TextRange textRange = new TextRange(SourceTextBox.Document.ContentStart, SourceTextBox.Document.ContentEnd);
            string text = textRange.Text;

            // Сохраняем текущую позицию каретки
            TextPointer caretPosition = SourceTextBox.CaretPosition;

            // Отключаем обработчики, чтобы избежать рекурсии
            SourceTextBox.TextChanged -= SourceTextBox_TextChanged;

            // Очищаем форматирование
            textRange.ClearAllProperties();

            // Используем лексический анализатор для разбора текста
            LexicalAnalyzer lexer = new LexicalAnalyzer(text, true);
            List<TokenInfo> tokens = GetTokens(lexer);

            // Применяем форматирование к каждому токену
            foreach (var token in tokens)
            {
                TextPointer start = GetTextPointerAtOffset(SourceTextBox.Document.ContentStart, token.StartIndex);
                TextPointer end = GetTextPointerAtOffset(SourceTextBox.Document.ContentStart, token.EndIndex);

                TextRange tokenRange = new TextRange(start, end);   

                // Применяем стиль в зависимости от типа токена
                switch (token.Lexem)
                {
                    case Lexems.Type:
                        tokenRange.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.CadetBlue);
                        break;
                    case Lexems.Keyword:
                        tokenRange.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Blue);
                        break;
                    case Lexems.String:
                    case Lexems.Char:
                        tokenRange.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Brown);
                        break;
                    case Lexems.Number:
                        tokenRange.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Magenta);
                        break;
                    case Lexems.Comment:
                        tokenRange.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Green);
                        break;
                    case Lexems.Error:
                        tokenRange.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.LightCoral);
                        break;
                    default:
                        tokenRange.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Black);
                        break;
                }
            }

            // Восстанавливаем позицию каретки
            SourceTextBox.CaretPosition = caretPosition;

            // Включаем обработчики обратно
            SourceTextBox.TextChanged += SourceTextBox_TextChanged;
        }

    }
}
