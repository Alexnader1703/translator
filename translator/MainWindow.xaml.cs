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
using System.Text.RegularExpressions;

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
       
        private TextPointer GetTextPointerAtOffset(TextPointer start, int offset)
        {
            //dfsdf
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
            // Сохраняем текущую позицию каретки
            TextPointer caretPosition = SourceTextBox.CaretPosition;

            // Отключаем обработчик события, чтобы избежать рекурсии
            SourceTextBox.TextChanged -= SourceTextBox_TextChanged;

            // Получаем весь текст из RichTextBox
            TextRange documentRange = new TextRange(SourceTextBox.Document.ContentStart, SourceTextBox.Document.ContentEnd);
            string text = documentRange.Text;

            // Очищаем все форматирование
            documentRange.ClearAllProperties();

            // Определяем кисти с указанными цветами
            SolidColorBrush constructsBrush = new SolidColorBrush(Color.FromRgb(180, 118, 175)); // rgb(180,118,175)
            SolidColorBrush typeBrush = new SolidColorBrush(Color.FromRgb(31, 91, 179));         // rgb(31,91,179)
            SolidColorBrush commentBrush = new SolidColorBrush(Color.FromRgb(31, 91, 179));      // тот же цвет, что и для типов
            SolidColorBrush variableBrush = new SolidColorBrush(Color.FromRgb(111, 202, 245));   // rgb(111,202,245)
            SolidColorBrush printBrush = new SolidColorBrush(Color.FromRgb(213, 202, 121));      // rgb(213,202,121)
            SolidColorBrush numberBrush = new SolidColorBrush(Color.FromRgb(174, 187, 116));     // rgb(174,187,116)
            SolidColorBrush beginEndBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0));     // Выбран оранжевый цвет для begin и end
            SolidColorBrush defaultBrush = Brushes.White; // Цвет по умолчанию

            // Устанавливаем цвет по умолчанию для всего текста
            documentRange.ApplyPropertyValue(TextElement.ForegroundProperty, defaultBrush);

            // Определяем паттерны для подсветки
            string constructsPattern = @"\b(if|else|elseif|endif|while|endwhile|for|do|case|ENDCASE|then|true|false)\b";
            string printPattern = @"\bprint\b";
            string beginEndPattern = @"\b(begin|end)\b";
            string typePattern = @"\b(int|bool|string|float|double|char|void)\b";
            string commentPattern = @"(\$\*[\s\S]*?\*\$)|(\$\$.*?$)";
            string numberPattern = @"\b\d+(\.\d+)?\b";
            string variablePattern = @"\b[A-Za-z_][A-Za-z0-9_]*\b";

            // Применяем подсветку для комментариев
            ApplySyntaxHighlighting(text, commentPattern, commentBrush, RegexOptions.Multiline);

            // Применяем подсветку для begin и end
            ApplySyntaxHighlighting(text, beginEndPattern, beginEndBrush);

            // Применяем подсветку для print
            ApplySyntaxHighlighting(text, printPattern, printBrush);

            // Применяем подсветку для конструкций
            ApplySyntaxHighlighting(text, constructsPattern, constructsBrush);

            // Применяем подсветку для типов данных
            ApplySyntaxHighlighting(text, typePattern, typeBrush);

            // Применяем подсветку для чисел
            ApplySyntaxHighlighting(text, numberPattern, numberBrush);

            // Применяем подсветку для переменных, исключая уже подсвеченные слова
            ApplySyntaxHighlighting(text, variablePattern, variableBrush, excludePatterns: new[] { constructsPattern, typePattern, beginEndPattern, printPattern });

            // Восстанавливаем позицию каретки
            SourceTextBox.CaretPosition = caretPosition;

            // Включаем обработчик обратно
            SourceTextBox.TextChanged += SourceTextBox_TextChanged;
        }

        private void ApplySyntaxHighlighting(string text, string pattern, Brush brush, RegexOptions options = RegexOptions.None, string[] excludePatterns = null)
        {
            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase | options);

            foreach (Match match in regex.Matches(text))
            {
                // Проверяем, не совпадает ли текущий матч с исключёнными паттернами
                if (excludePatterns != null)
                {
                    bool isExcluded = false;
                    foreach (var excludePattern in excludePatterns)
                    {
                        if (Regex.IsMatch(match.Value, excludePattern, RegexOptions.IgnoreCase))
                        {
                            isExcluded = true;
                            break;
                        }
                    }
                    if (isExcluded)
                        continue;
                }

                TextPointer start = GetTextPositionAtOffset(SourceTextBox.Document.ContentStart, match.Index);
                TextPointer end = GetTextPositionAtOffset(SourceTextBox.Document.ContentStart, match.Index + match.Length);

                TextRange range = new TextRange(start, end);
                range.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
            }
        }

        private TextPointer GetTextPositionAtOffset(TextPointer start, int offset)
        {
            TextPointer current = start;
            int currentOffset = 0;

            while (current != null)
            {
                if (current.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string text = current.GetTextInRun(LogicalDirection.Forward);
                    int textLength = text.Length;

                    if (currentOffset + textLength >= offset)
                    {
                        return current.GetPositionAtOffset(offset - currentOffset);
                    }

                    currentOffset += textLength;
                    current = current.GetPositionAtOffset(textLength);
                }
                else
                {
                    current = current.GetNextContextPosition(LogicalDirection.Forward);
                }
            }

            return start;
        }



    }
}
