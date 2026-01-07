using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Data.Entity;
using Word = Microsoft.Office.Interop.Word;

namespace Genio
{
    public partial class ReportsPage : Page
    {
        private List<string> selectedSpecialties = new List<string>();
        private List<int> selectedCourses = new List<int>();
        private DateTime startDate = new DateTime(2024, 9, 1);
        private DateTime endDate = new DateTime(2025, 5, 15);
        private bool isReportGenerated = false;
        private TextBox currentDateTextBox = null;

        public ReportsPage()
        {
            InitializeComponent();
            LoadDefaultSelections();
            UpdateExportButtonsState();

            DatePickerCalendar.SelectedDatesChanged += DatePickerCalendar_SelectedDatesChanged;
            UpdateDateTexts();
        }

        private void LoadDefaultSelections()
        {
            if (Spec1 != null) Spec1.IsChecked = true;
            if (Spec2 != null) Spec2.IsChecked = true;
            if (Spec3 != null) Spec3.IsChecked = true;
            if (Spec4 != null) Spec4.IsChecked = true;
            if (Spec5 != null) Spec5.IsChecked = true;
            if (Spec6 != null) Spec6.IsChecked = true;
            if (Spec7 != null) Spec7.IsChecked = true;

            if (Course1 != null) Course1.IsChecked = true;
            if (Course2 != null) Course2.IsChecked = true;
            if (Course3 != null) Course3.IsChecked = true;
            if (Course4 != null) Course4.IsChecked = true;
        }

        private void UpdateDateTexts()
        {
            DateFromText.Text = startDate.ToString("dd.MM.yyyy");
            DateToText.Text = endDate.ToString("dd.MM.yyyy");
        }

        private void UpdateExportButtonsState()
        {
            ExportWordBtn.IsEnabled = isReportGenerated;

            if (isReportGenerated)
            {
                ExportWordBtn.Style = (Style)FindResource("AccentButtonStyle");
            }
            else
            {
                ExportWordBtn.Style = (Style)FindResource("DisabledButtonStyle");
            }
        }

        private void GenerateBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateSelectedSpecialties();
                UpdateSelectedCourses();

                DisplayReportInFlowDocument();

                isReportGenerated = true;
                UpdateExportButtonsState();

                MessageBox.Show("Отчет успешно сформирован!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisplayReportInFlowDocument()
        {
            if (ReportDocument == null) return;

            ReportDocument.Blocks.Clear();

            // Заголовок отчета
            var title = new System.Windows.Documents.Paragraph
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            title.Inlines.Add(new Run("ОТЧЕТ ПО ОЛИМПИАДАМ"));
            ReportDocument.Blocks.Add(title);

            // Подзаголовок
            var subtitle = new System.Windows.Documents.Paragraph
            {
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            subtitle.Inlines.Add(new Run("Сводный по олимпиадам"));
            ReportDocument.Blocks.Add(subtitle);

            // Информация о периоде
            var periodInfo = new System.Windows.Documents.Paragraph
            {
                FontSize = 12,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            periodInfo.Inlines.Add(new Run($"Период: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}"));
            ReportDocument.Blocks.Add(periodInfo);

            // Дата формирования
            var dateGenerated = new System.Windows.Documents.Paragraph
            {
                FontSize = 11,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            dateGenerated.Inlines.Add(new Run($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}"));
            ReportDocument.Blocks.Add(dateGenerated);

            // Курсы и специальности
            var coursesStr = selectedCourses.Any() ? string.Join(", ", selectedCourses) : "Все";
            var specsStr = selectedSpecialties.Any() ? string.Join(", ", selectedSpecialties) : "Все";

            var filtersInfo = new System.Windows.Documents.Paragraph
            {
                FontSize = 11,
                TextAlignment = TextAlignment.Left,
                Margin = new Thickness(0, 0, 0, 20)
            };
            filtersInfo.Inlines.Add(new Run($"Курсы: {coursesStr}"));
            filtersInfo.Inlines.Add(new LineBreak());
            filtersInfo.Inlines.Add(new Run($"Специальности: {specsStr}"));
            ReportDocument.Blocks.Add(filtersInfo);

            try
            {
                using (var db = new GenioAppEntities())
                {
                    var participations = GetFilteredParticipations(db);
                    DisplayOlympiadTableInFlowDocument(participations);
                }
            }
            catch (Exception ex)
            {
                var errorPara = new System.Windows.Documents.Paragraph
                {
                    FontSize = 10,
                    Margin = new Thickness(0, 10, 0, 5),
                    Foreground = Brushes.Red
                };
                errorPara.Inlines.Add(new Run($"Ошибка при формировании отчета: {ex.Message}"));
                ReportDocument.Blocks.Add(errorPara);
            }
        }

        private void DisplayOlympiadTableInFlowDocument(List<StudentOlimp> participations)
        {
            // Таблица для предварительного просмотра
            var table = new System.Windows.Documents.Table();
            table.CellSpacing = 0;
            table.Background = Brushes.White;
            table.Margin = new Thickness(0, 10, 0, 20);

            // Колонки
            table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(150, GridUnitType.Pixel) });
            table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(100, GridUnitType.Pixel) });
            table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(80, GridUnitType.Pixel) });
            table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(120, GridUnitType.Pixel) });

            // Заголовки столбцов
            var headerRowGroup = new System.Windows.Documents.TableRowGroup();
            var headerRow = new System.Windows.Documents.TableRow();
            headerRow.Background = Brushes.LightGray;

            string[] headers = { "Название", "Учащиеся", "Результат", "Номинация" };
            foreach (var header in headers)
            {
                var headerCell = new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(
                    new Run(header)));
                headerCell.FontWeight = FontWeights.Bold;
                headerCell.TextAlignment = TextAlignment.Center;
                headerCell.Padding = new Thickness(5);
                headerCell.BorderBrush = Brushes.Black;
                headerCell.BorderThickness = new Thickness(1);
                headerRow.Cells.Add(headerCell);
            }
            headerRowGroup.Rows.Add(headerRow);

            // Данные таблицы
            var dataRowGroup = new System.Windows.Documents.TableRowGroup();

            if (!participations.Any())
            {
                var emptyRow = new System.Windows.Documents.TableRow();
                var emptyCell = new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(
                    new Run("Нет данных по выбранным критериям")));
                emptyCell.ColumnSpan = 4;
                emptyCell.TextAlignment = TextAlignment.Center;
                emptyCell.Padding = new Thickness(5);
                emptyCell.FontStyle = FontStyles.Italic;
                emptyRow.Cells.Add(emptyCell);
                dataRowGroup.Rows.Add(emptyRow);
            }
            else
            {
                // Группировка по олимпиадам
                var groupedByOlympiad = participations
                    .GroupBy(p => p.Olimp)
                    .OrderByDescending(g => g.Key.olimp_date)
                    .ToList();

                foreach (var group in groupedByOlympiad)
                {
                    var olimp = group.Key;
                    var students = group.ToList();

                    // Строки с данными для каждого студента
                    bool isFirstStudent = true;
                    foreach (var participation in students)
                    {
                        var dataRow = new System.Windows.Documents.TableRow();

                        // Название олимпиады
                        var nameCell = new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(
                            new Run(isFirstStudent ? olimp.olimp_name : ""))); // Run (пробег/кусок текста)
                        nameCell.Padding = new Thickness(5);
                        nameCell.BorderBrush = Brushes.Black;
                        nameCell.BorderThickness = new Thickness(1);
                        if (isFirstStudent)
                        {
                            nameCell.FontWeight = FontWeights.Bold;
                            nameCell.Background = Brushes.LightYellow;
                        }
                        dataRow.Cells.Add(nameCell);

                        // Учащийся
                        var studentName = $"{participation.Student.last_name} {participation.Student.first_name}";
                        var studentCell = new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(
                            new Run(studentName)));
                        studentCell.Padding = new Thickness(5);
                        studentCell.BorderBrush = Brushes.Black;
                        studentCell.BorderThickness = new Thickness(1);
                        dataRow.Cells.Add(studentCell);

                        // Результат
                        var resultCell = new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(
                            new Run(participation.result)));
                        resultCell.Padding = new Thickness(5);
                        resultCell.BorderBrush = Brushes.Black;
                        resultCell.BorderThickness = new Thickness(1);
                        resultCell.TextAlignment = TextAlignment.Center;

                        // Цвет для призовых мест
                        if (participation.result.Contains("1 место") ||
                            participation.result.Contains("2 место") ||
                            participation.result.Contains("3 место") ||
                            participation.result.Contains("Победитель"))
                        {
                            resultCell.Background = Brushes.LightGreen;
                        }
                        dataRow.Cells.Add(resultCell);

                        // Номинация
                        var nominations = olimp.nominations ?? "";
                        var firstNomination = nominations.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .FirstOrDefault() ?? "";
                        var nominationCell = new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(
                            new Run(firstNomination.Trim())));
                        nominationCell.Padding = new Thickness(5);
                        nominationCell.BorderBrush = Brushes.Black;
                        nominationCell.BorderThickness = new Thickness(1);
                        dataRow.Cells.Add(nominationCell);

                        dataRowGroup.Rows.Add(dataRow);
                        isFirstStudent = false;
                    }
                }
            }

            // Добавляем группы строк в таблицу
            table.RowGroups.Add(headerRowGroup);
            table.RowGroups.Add(dataRowGroup);

            ReportDocument.Blocks.Add(table);

            // Статистика в конце
            var totalOlimps = participations.Select(p => p.olimp_id).Distinct().Count();
            var totalStudents = participations.Select(p => p.student_id).Distinct().Count();
            var prizePlaces = participations.Count(p =>
                p.result.Contains("1 место") ||
                p.result.Contains("2 место") ||
                p.result.Contains("3 место") ||
                p.result.Contains("Победитель"));

            var stats = new System.Windows.Documents.Paragraph
            {
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 20, 0, 5)
            };
            stats.Inlines.Add(new Run("СТАТИСТИКА:"));
            ReportDocument.Blocks.Add(stats);

            var statsList = new System.Windows.Documents.List
            {
                MarkerStyle = TextMarkerStyle.Disc,
                Margin = new Thickness(20, 0, 0, 10)
            };

            statsList.ListItems.Add(new System.Windows.Documents.ListItem(
                new System.Windows.Documents.Paragraph(new Run($"Количество олимпиад в период: {totalOlimps}"))));
            statsList.ListItems.Add(new System.Windows.Documents.ListItem(
                new System.Windows.Documents.Paragraph(new Run($"Количество учащихся: {totalStudents}"))));
            statsList.ListItems.Add(new System.Windows.Documents.ListItem(
                new System.Windows.Documents.Paragraph(new Run($"Количество призовых мест: {prizePlaces}"))));

            ReportDocument.Blocks.Add(statsList);
        }

        private List<StudentOlimp> GetFilteredParticipations(GenioAppEntities db)
        {
            var query = db.StudentOlimps
                .Include("Student")
                .Include("Olimp")
                .Include("Student.Specialization")
                .Where(so => so.Olimp.olimp_date >= startDate && so.Olimp.olimp_date <= endDate)
                .AsQueryable();

            // Фильтруем по курсам
            if (selectedCourses.Any())
            {
                query = query.Where(so => selectedCourses.Contains(so.Student.course_number));
            }

            // Фильтруем по специальностям
            if (selectedSpecialties.Any())
            {
                query = query.Where(so => selectedSpecialties.Contains(so.Student.Specialization.spec_name));
            }

            return query.OrderByDescending(so => so.Olimp.olimp_date).ToList();
        }

        private void ExportWordBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!isReportGenerated)
            {
                MessageBox.Show("Сначала сформируйте отчет!", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                SaveFileDialog dialog = new SaveFileDialog
                {
                    Filter = "Word документы (*.docx)|*.docx",
                    FileName = $"Отчет_олимпиады_{DateTime.Now:yyyyMMdd_HHmm}.docx",
                    DefaultExt = ".docx"
                };

                if (dialog.ShowDialog() == true)
                {
                    ExportToWordDocument(dialog.FileName);
                    MessageBox.Show($"Word отчет успешно сохранен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте в Word: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToWordDocument(string filePath)
        {
            Word.Application wordApp = null;
            Word.Document wordDoc = null;

            try
            {
                wordApp = new Word.Application();
                wordApp.Visible = false;

                // Создаем новый документ
                wordDoc = wordApp.Documents.Add();

                // Устанавливаем поля (в сантиметрах)
                wordDoc.PageSetup.LeftMargin = wordApp.CentimetersToPoints(2.5f);
                wordDoc.PageSetup.RightMargin = wordApp.CentimetersToPoints(2.5f);
                wordDoc.PageSetup.TopMargin = wordApp.CentimetersToPoints(2.5f);
                wordDoc.PageSetup.BottomMargin = wordApp.CentimetersToPoints(2.5f);

                // Получаем данные для отчета
                using (var db = new GenioAppEntities())
                {
                    var participations = GetFilteredParticipations(db);

                    // Заголовок отчета
                    Word.Paragraph titlePara = wordDoc.Paragraphs.Add();
                    titlePara.Range.Text = "ОТЧЕТ ПО ОЛИМПИАДАМ";
                    titlePara.Range.Font.Size = 16;
                    titlePara.Range.Font.Bold = 1;
                    titlePara.Format.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                    titlePara.Range.InsertParagraphAfter();

                    // Подзаголовок
                    Word.Paragraph subtitlePara = wordDoc.Paragraphs.Add();
                    subtitlePara.Range.Text = "Сводный по олимпиадам";
                    subtitlePara.Range.Font.Size = 14;
                    subtitlePara.Range.Font.Bold = 1;
                    subtitlePara.Format.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                    subtitlePara.Range.InsertParagraphAfter();

                    // Информация о периоде
                    Word.Paragraph periodPara = wordDoc.Paragraphs.Add();
                    periodPara.Range.Text = $"Период: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}";
                    periodPara.Range.Font.Size = 12;
                    periodPara.Format.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                    periodPara.Range.InsertParagraphAfter();

                    // Дата формирования
                    Word.Paragraph datePara = wordDoc.Paragraphs.Add();
                    datePara.Range.Text = $"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}";
                    datePara.Range.Font.Size = 11;
                    datePara.Format.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                    datePara.Range.InsertParagraphAfter();

                    // Курсы и специальности
                    var coursesStr = selectedCourses.Any() ? string.Join(", ", selectedCourses) : "Все";
                    var specsStr = selectedSpecialties.Any() ? string.Join(", ", selectedSpecialties) : "Все";

                    Word.Paragraph filtersPara = wordDoc.Paragraphs.Add();
                    filtersPara.Range.Text = $"Курсы: {coursesStr}";
                    filtersPara.Range.Font.Size = 11;
                    filtersPara.Format.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
                    filtersPara.Range.InsertParagraphAfter();

                    Word.Paragraph specsPara = wordDoc.Paragraphs.Add();
                    specsPara.Range.Text = $"Специальности: {specsStr}";
                    specsPara.Range.Font.Size = 11;
                    specsPara.Format.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
                    specsPara.Range.InsertParagraphAfter();

                    // Пустая строка перед таблицей
                    wordDoc.Paragraphs.Add().Range.InsertParagraphAfter();

                    // Создаем таблицу в Word
                    if (participations.Any())
                    {
                        // Группируем по олимпиадам
                        var groupedByOlympiad = participations
                            .GroupBy(p => p.Olimp)
                            .OrderByDescending(g => g.Key.olimp_date)
                            .ToList();

                        // Определяем количество строк для таблицы
                        int totalRows = 1; // Заголовок
                        foreach (var group in groupedByOlympiad)
                        {
                            totalRows += group.Count(); // Строки для каждого студента
                        }

                        // Создаем таблицу
                        Word.Table wordTable = wordDoc.Tables.Add(
                            wordDoc.Range(wordDoc.Paragraphs.Last.Range.Start, wordDoc.Paragraphs.Last.Range.Start),
                            totalRows,
                            4);

                        // Настраиваем таблицу
                        wordTable.Borders.OutsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
                        wordTable.Borders.OutsideLineWidth = Word.WdLineWidth.wdLineWidth050pt;
                        wordTable.Borders.InsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
                        wordTable.Borders.InsideLineWidth = Word.WdLineWidth.wdLineWidth025pt;

                        // Заголовки столбцов
                        string[] headers = { "Название", "Учащиеся", "Результат", "Номинация" };
                        for (int i = 0; i < headers.Length; i++)
                        {
                            wordTable.Cell(1, i + 1).Range.Text = headers[i];
                            wordTable.Cell(1, i + 1).Range.Font.Bold = 1;
                            wordTable.Cell(1, i + 1).Range.Font.Size = 11;
                            wordTable.Cell(1, i + 1).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                            wordTable.Cell(1, i + 1).Shading.BackgroundPatternColor = Word.WdColor.wdColorGray15;
                        }

                        // Заполняем таблицу данными
                        int currentRow = 2;
                        foreach (var group in groupedByOlympiad)
                        {
                            var olimp = group.Key;
                            var students = group.ToList();

                            bool isFirstStudent = true;
                            foreach (var participation in students)
                            {
                                var studentName = $"{participation.Student.last_name} {participation.Student.first_name}";

                                // Название олимпиады
                                wordTable.Cell(currentRow, 1).Range.Text = isFirstStudent ? olimp.olimp_name : "";
                                if (isFirstStudent)
                                {
                                    wordTable.Cell(currentRow, 1).Range.Font.Bold = 1;
                                    wordTable.Cell(currentRow, 1).Shading.BackgroundPatternColor = Word.WdColor.wdColorLightYellow;
                                }

                                // Учащийся
                                wordTable.Cell(currentRow, 2).Range.Text = studentName;

                                // Результат
                                wordTable.Cell(currentRow, 3).Range.Text = participation.result;
                                wordTable.Cell(currentRow, 3).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;

                                // Цвет для призовых мест
                                if (participation.result.Contains("1 место") ||
                                    participation.result.Contains("2 место") ||
                                    participation.result.Contains("3 место") ||
                                    participation.result.Contains("Победитель"))
                                {
                                    wordTable.Cell(currentRow, 3).Shading.BackgroundPatternColor = Word.WdColor.wdColorBrightGreen;
                                }

                                // Номинация
                                var nominations = olimp.nominations ?? "";
                                var firstNomination = nominations.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                    .FirstOrDefault() ?? "";
                                wordTable.Cell(currentRow, 4).Range.Text = firstNomination.Trim();

                                // Настройка всех ячеек в строке
                                for (int i = 1; i <= 4; i++)
                                {
                                    wordTable.Cell(currentRow, i).Range.Font.Size = 10;
                                    wordTable.Cell(currentRow, i).VerticalAlignment = Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter;
                                }

                                currentRow++;
                                isFirstStudent = false;
                            }
                        }

                        // Автоподбор ширины столбцов
                        wordTable.AutoFitBehavior(Word.WdAutoFitBehavior.wdAutoFitWindow);
                    }

                    // Пустая строка после таблицы
                    wordDoc.Paragraphs.Add().Range.InsertParagraphAfter();

                    // Статистика
                    var totalOlimps = participations.Select(p => p.olimp_id).Distinct().Count();
                    var totalStudents = participations.Select(p => p.student_id).Distinct().Count();
                    var prizePlaces = participations.Count(p =>
                        p.result.Contains("1 место") ||
                        p.result.Contains("2 место") ||
                        p.result.Contains("3 место") ||
                        p.result.Contains("Победитель"));

                    Word.Paragraph statsTitlePara = wordDoc.Paragraphs.Add();
                    statsTitlePara.Range.Text = "СТАТИСТИКА:";
                    statsTitlePara.Range.Font.Bold = 1;
                    statsTitlePara.Range.Font.Size = 12;
                    statsTitlePara.Range.InsertParagraphAfter();

                    Word.Paragraph statsPara1 = wordDoc.Paragraphs.Add();
                    statsPara1.Range.Text = $"• Количество олимпиад в период: {totalOlimps}";
                    statsPara1.Format.LeftIndent = wordApp.CentimetersToPoints(0.5f);
                    statsPara1.Range.Font.Size = 11;
                    statsPara1.Range.InsertParagraphAfter();

                    Word.Paragraph statsPara2 = wordDoc.Paragraphs.Add();
                    statsPara2.Range.Text = $"• Количество учащихся: {totalStudents}";
                    statsPara2.Format.LeftIndent = wordApp.CentimetersToPoints(0.5f);
                    statsPara2.Range.Font.Size = 11;
                    statsPara2.Range.InsertParagraphAfter();

                    Word.Paragraph statsPara3 = wordDoc.Paragraphs.Add();
                    statsPara3.Range.Text = $"• Количество призовых мест: {prizePlaces}";
                    statsPara3.Format.LeftIndent = wordApp.CentimetersToPoints(0.5f);
                    statsPara3.Range.Font.Size = 11;
                    statsPara3.Range.InsertParagraphAfter();
                }

                // Сохраняем документ
                object fileName = filePath;
                wordDoc.SaveAs2(ref fileName);
                wordDoc.Close();
                wordApp.Quit();

                // Освобождаем COM-объекты
                System.Runtime.InteropServices.Marshal.ReleaseComObject(wordDoc);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(wordApp);
            }
            catch (Exception ex)
            {
                // Закрываем документ и приложение в случае ошибки
                if (wordDoc != null)
                {
                    wordDoc.Close(false);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(wordDoc);
                }
                if (wordApp != null)
                {
                    wordApp.Quit();
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(wordApp);
                }

                throw new Exception($"Ошибка при создании Word документа: {ex.Message}");
            }
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Очистить все параметры?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                if (Spec1 != null) Spec1.IsChecked = false;
                if (Spec2 != null) Spec2.IsChecked = false;
                if (Spec3 != null) Spec3.IsChecked = false;
                if (Spec4 != null) Spec4.IsChecked = false;
                if (Spec5 != null) Spec5.IsChecked = false;
                if (Spec6 != null) Spec6.IsChecked = false;
                if (Spec7 != null) Spec7.IsChecked = false;

                if (Course1 != null) Course1.IsChecked = false;
                if (Course2 != null) Course2.IsChecked = false;
                if (Course3 != null) Course3.IsChecked = false;
                if (Course4 != null) Course4.IsChecked = false;

                startDate = new DateTime(2024, 9, 1);
                endDate = new DateTime(2025, 5, 15);
                UpdateDateTexts();

                isReportGenerated = false;
                UpdateExportButtonsState();

                if (ReportDocument != null)
                    ReportDocument.Blocks.Clear();
            }
        }

        private void SpecialtyCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateSelectedSpecialties();
        }

        private void CourseCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateSelectedCourses();
        }

        private void UpdateSelectedSpecialties()
        {
            selectedSpecialties.Clear();

            if (Spec1 != null && Spec1.IsChecked == true)
                selectedSpecialties.Add(Spec1.Content.ToString());
            if (Spec2 != null && Spec2.IsChecked == true)
                selectedSpecialties.Add(Spec2.Content.ToString());
            if (Spec3 != null && Spec3.IsChecked == true)
                selectedSpecialties.Add(Spec3.Content.ToString());
            if (Spec4 != null && Spec4.IsChecked == true)
                selectedSpecialties.Add(Spec4.Content.ToString());
            if (Spec5 != null && Spec5.IsChecked == true)
                selectedSpecialties.Add(Spec5.Content.ToString());
            if (Spec6 != null && Spec6.IsChecked == true)
                selectedSpecialties.Add(Spec6.Content.ToString());
            if (Spec7 != null && Spec7.IsChecked == true)
                selectedSpecialties.Add(Spec7.Content.ToString());
        }

        private void UpdateSelectedCourses()
        {
            selectedCourses.Clear();

            if (Course1 != null && Course1.IsChecked == true) selectedCourses.Add(1);
            if (Course2 != null && Course2.IsChecked == true) selectedCourses.Add(2);
            if (Course3 != null && Course3.IsChecked == true) selectedCourses.Add(3);
            if (Course4 != null && Course4.IsChecked == true) selectedCourses.Add(4);
        }

        private void DateFromButton_Click(object sender, RoutedEventArgs e)
        {
            currentDateTextBox = DateFromText;
            ShowDatePicker(startDate);
        }

        private void DateToButton_Click(object sender, RoutedEventArgs e)
        {
            currentDateTextBox = DateToText;
            ShowDatePicker(endDate);
        }

        private void ShowDatePicker(DateTime initialDate)
        {
            DatePickerCalendar.SelectedDate = initialDate;
            DatePickerCalendar.DisplayDate = initialDate;

            if (currentDateTextBox == DateFromText)
            {
                DatePickerPopup.PlacementTarget = DateFromButton;
            }
            else
            {
                DatePickerPopup.PlacementTarget = DateToButton;
            }

            DatePickerPopup.IsOpen = true;
        }

        private void DatePickerCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatePickerCalendar.SelectedDate.HasValue && currentDateTextBox != null)
            {
                var selectedDate = DatePickerCalendar.SelectedDate.Value;

                if (currentDateTextBox.Name == "DateFromText")
                {
                    startDate = selectedDate;
                }
                else if (currentDateTextBox.Name == "DateToText")
                {
                    endDate = selectedDate;
                }

                currentDateTextBox.Text = selectedDate.ToString("dd.MM.yyyy");
                DatePickerPopup.IsOpen = false;
            }
        }
    }
}