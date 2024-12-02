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

        private bool isSyntaxHighlightingEnabled = true; // Подсветка включена по умолчанию

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

            // Добавляем обработчик для события PreviewKeyUp
            SourceTextBox.PreviewKeyUp += SourceTextBox_PreviewKeyUp;
            SourceTextBox.PreviewKeyDown += SourceTextBox_PreviewKeyDown;
            SourceTextBox.PreviewTextInput += SourceTextBox_PreviewTextInput;

            // Инициализируем кнопку подсветки
            HighlightSyntaxButton.ToolTip = "Отключить подсветку";
            HighlightSyntaxButton.Click += HighlightSyntax_Click;
        }
        private void SourceTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                // Отменяем стандартное поведение Tab
                e.Handled = true;

                TextSelection selection = SourceTextBox.Selection;

                if (!selection.IsEmpty)
                {
                    // Если есть выделенный текст, заменяем его на 3 пробела
                    selection.Text = "    ";
                    // Перемещаем каретку в конец вставленных пробелов
                    SourceTextBox.CaretPosition = selection.End;
                }
                else
                {
                    // Вставляем 3 пробела в текущую позицию каретки
                    TextPointer caret = SourceTextBox.CaretPosition;

                    // Создаём TextRange для вставки текста
                    TextRange tr = new TextRange(caret, caret);
                    tr.Text = "    ";

                    // Перемещаем каретку в конец вставленных пробелов
                    SourceTextBox.CaretPosition = tr.End;
                }
            }
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

                    // Применяем подсветку после загрузки файла, если она включена
                    if (isSyntaxHighlightingEnabled)
                    {
                        ApplySyntaxHighlighting();
                    }
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

        private void ApplySyntaxHighlighting()
        {
            if (!isSyntaxHighlightingEnabled)
                return; // Если подсветка отключена, выходим из метода

            // Сохраняем позицию каретки как смещение от начала документа
            int caretOffset = GetTextLength(SourceTextBox.Document.ContentStart, SourceTextBox.CaretPosition);

            // Получаем весь текст с нормализованными переносами строк
            TextRange documentRange = new TextRange(SourceTextBox.Document.ContentStart, SourceTextBox.Document.ContentEnd);
            string text = documentRange.Text.Replace("\r\n", "\n");

            // Очищаем документ и создаем новый с единообразной структурой
            SourceTextBox.Document.Blocks.Clear();
            Paragraph paragraph = new Paragraph();
            SourceTextBox.Document.Blocks.Add(paragraph);

            // Определяем кисти для подсветки
            SolidColorBrush constructsBrush = new SolidColorBrush(Color.FromRgb(180, 118, 175));
            SolidColorBrush typeBrush = new SolidColorBrush(Color.FromRgb(31, 91, 179));
            SolidColorBrush commentBrush = new SolidColorBrush(Color.FromRgb(31, 91, 179));
            SolidColorBrush variableBrush = new SolidColorBrush(Color.FromRgb(111, 202, 245));
            SolidColorBrush printBrush = new SolidColorBrush(Color.FromRgb(230, 220, 123));
            SolidColorBrush numberBrush = new SolidColorBrush(Color.FromRgb(174, 187, 116));
            SolidColorBrush beginEndBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0));
            SolidColorBrush defaultBrush = Brushes.White;

            // Паттерны для подсветки
            string constructsPattern = @"\b(if|else|elseif|endif|while|endwhile|for|do|case|ENDCASE|then|true|false|OF)\b";
            string printPattern = @"\bprint\b";
            string beginEndPattern = @"\b(begin|end)\b";
            string typePattern = @"\b(int|bool|string|float|double|char|void)\b";
            string commentPattern = @"(\$\*[\s\S]*?\*\$)|(\$\$.*?$)";
            string numberPattern = @"\b\d+(\.\d+)?\b";
            string variablePattern = @"\b[A-Za-z_][A-Za-z0-9_]*\b";

            // Создаем список инлайнов
            List<Inline> inlines = new List<Inline>();
            
            // Объединяем все паттерны в один с именованными группами
            string pattern = $@"(?<Comment>{commentPattern})|
                                (?<Constructs>{constructsPattern})|
                                (?<Type>{typePattern})|
                                (?<BeginEnd>{beginEndPattern})|
                                (?<Print>{printPattern})|
                                (?<Number>{numberPattern})|
                                (?<Variable>{variablePattern})";

            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline);

            int lastIndex = 0;

            foreach (Match match in regex.Matches(text))
            {
                // Добавляем текст до совпадения
                if (match.Index > lastIndex)
                {
                    string beforeText = text.Substring(lastIndex, match.Index - lastIndex);
                    Run beforeRun = new Run(beforeText) { Foreground = defaultBrush };
                    inlines.Add(beforeRun);
                }

                // Определяем тип совпадения и устанавливаем соответствующий цвет
                Brush brush = defaultBrush;

                if (match.Groups["Comment"].Success)
                    brush = commentBrush;
                else if (match.Groups["Constructs"].Success)
                    brush = constructsBrush;
                else if (match.Groups["Type"].Success)
                    brush = typeBrush;
                else if (match.Groups["BeginEnd"].Success)
                    brush = beginEndBrush;
                else if (match.Groups["Print"].Success)
                    brush = printBrush;
                else if (match.Groups["Number"].Success)
                    brush = numberBrush;
                else if (match.Groups["Variable"].Success)
                    brush = variableBrush;

                Run matchRun = new Run(match.Value) { Foreground = brush };
                inlines.Add(matchRun);

                lastIndex = match.Index + match.Length;
            }

            // Добавляем оставшийся текст после последнего совпадения
            if (lastIndex < text.Length)
            {
                string afterText = text.Substring(lastIndex);
                Run afterRun = new Run(afterText) { Foreground = defaultBrush };
                inlines.Add(afterRun);
            }

            // Очищаем и заполняем параграф новыми инлайнами
            paragraph.Inlines.Clear();
            foreach (var inline in inlines)
            {
                paragraph.Inlines.Add(inline);
            }

            // Восстанавливаем позицию каретки
            TextPointer caretPosition = GetTextPositionAtOffset(SourceTextBox.Document.ContentStart, caretOffset);
            SourceTextBox.CaretPosition = caretPosition;

            // Фокусируемся на текстовом поле
            SourceTextBox.Focus();
        }

        private void RemoveSyntaxHighlighting()
        {
            // Сохраняем позицию каретки
            int caretOffset = GetTextLength(SourceTextBox.Document.ContentStart, SourceTextBox.CaretPosition);

            // Получаем весь текст без изменений
            TextRange documentRange = new TextRange(SourceTextBox.Document.ContentStart, SourceTextBox.Document.ContentEnd);
            string text = documentRange.Text.Replace("\r\n", "\n");

            // Очищаем документ и создаем новый параграф с текстом
            SourceTextBox.Document.Blocks.Clear();
            Paragraph paragraph = new Paragraph();
            SourceTextBox.Document.Blocks.Add(paragraph);

            Run run = new Run(text) { Foreground = Brushes.White };
            paragraph.Inlines.Add(run);

            // Восстанавливаем позицию каретки
            TextPointer caretPosition = GetTextPositionAtOffset(SourceTextBox.Document.ContentStart, caretOffset);
            SourceTextBox.CaretPosition = caretPosition;

            // Фокусируемся на текстовом поле
            SourceTextBox.Focus();
        }

        private int GetTextLength(TextPointer start, TextPointer end)
        {
            return new TextRange(start, end).Text.Replace("\r\n", "\n").Length;
        }

        private TextPointer GetTextPositionAtOffset(TextPointer start, int offset)
        {
            TextPointer current = start;
            int count = 0;

            while (current != null)
            {
                if (current.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string textRun = current.GetTextInRun(LogicalDirection.Forward).Replace("\r\n", "\n");
                    int runLength = textRun.Length;

                    if (count + runLength >= offset)
                    {
                        return current.GetPositionAtOffset(offset - count, LogicalDirection.Forward);
                    }

                    count += runLength;
                    current = current.GetPositionAtOffset(runLength, LogicalDirection.Forward);
                }
                else
                {
                    current = current.GetNextContextPosition(LogicalDirection.Forward);
                }
            }

            return start;
        }
        private void CloseApp_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void HighlightSyntax_Click(object sender, RoutedEventArgs e)
        {
            isSyntaxHighlightingEnabled = !isSyntaxHighlightingEnabled; // Переключаем состояние

            // Обновляем текст кнопки
            if (isSyntaxHighlightingEnabled)
            {
                HighlightSyntaxButton.ToolTip = "Отключить подсветку";
                ApplySyntaxHighlighting(); // Применяем подсветку при включении
            }
            else
            {
                HighlightSyntaxButton.ToolTip = "Включить подсветку";
                RemoveSyntaxHighlighting(); // Убираем подсветку при отключении
            }
        }
        private void SourceTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (isSyntaxHighlightingEnabled)
            {
                // Символы, после которых нужно вызвать подсветку
                char[] triggerChars = { ';', ':', ',', '.', '(', ')', '{', '}', '[', ']', '=', '<', '>', '+', '-', '*', '/' };

                if (triggerChars.Contains(e.Text.Last()))
                {
                    ApplySyntaxHighlighting();
                }
            }
        }

        private void SourceTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (isSyntaxHighlightingEnabled && (e.Key == Key.Space || e.Key == Key.Enter))
            {
                ApplySyntaxHighlighting();
            }
        }

    }
}
