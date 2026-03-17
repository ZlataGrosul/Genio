using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Genio
{
    public partial class HonorBoardPage : Page
    {
        private bool filtersVisible = false;
        private List<int> selectedSpecialties = new List<int>();
        private List<int> selectedCourses = new List<int>();
        private int selectedYear = 0;
        private string currentSearchText = "";
        private Dictionary<CheckBox, int> specialtyCheckBoxes = new Dictionary<CheckBox, int>();
        private List<HonorDisplayItem> allHonorItems = new List<HonorDisplayItem>();
        private ICollectionView honorItemsView;

        public class HonorDisplayItem
        {
            public string FullName { get; set; }
            public string LastName { get; set; }
            public string FirstName { get; set; }
            public string MiddleName { get; set; }
            public int Course { get; set; }
            public string Group { get; set; }
            public DateTime AdmissionDate { get; set; }
            public DateTime GraduationDate { get; set; }
            public DateTime PlacementDate { get; set; }
            public int StudentId { get; set; }
            public int HonorId { get; set; }
        }

        public HonorBoardPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadYearComboBox();
            LoadSpecialtiesFromDatabase();
            LoadDefaultFilters();
            LoadHonorBoardData();
        }

        private void LoadYearComboBox()
        {
            try
            {
                using (var db = new GenioAppEntities())
                {
                    YearComboBox.Items.Clear();
                    YearComboBox.Items.Add(new ComboBoxItem { Content = "За всё время", Tag = 0 });

                    var honorRecords = db.HonorBoard_GetAll();
                    var years = honorRecords
                        .Select(h => h.placement_date.Year)
                        .Distinct()
                        .OrderByDescending(y => y)
                        .ToList();

                    foreach (var year in years)
                    {
                        YearComboBox.Items.Add(new ComboBoxItem { Content = year.ToString(), Tag = year });
                    }
                    YearComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка загрузки списка годов: {ex.Message}", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
            }
        }

        private void LoadSpecialtiesFromDatabase()
        {
            try
            {
                using (var db = new GenioAppEntities())
                {
                    var specialties = db.Specializations_GetAll();

                    SpecialtiesFilterPanel.Children.Clear();
                    specialtyCheckBoxes.Clear();

                    foreach (var specialty in specialties)
                    {
                        var checkBox = new CheckBox
                        {
                            Content = specialty.spec_name,
                            Margin = new Thickness(0, 0, 0, 6),
                            Style = (Style)FindResource("ReportCheckBoxStyle"),
                            FontSize = 12,
                            Tag = specialty.specialization_id
                        };
                        checkBox.Checked += SpecialtyFilter_Changed;
                        checkBox.Unchecked += SpecialtyFilter_Changed;
                        specialtyCheckBoxes.Add(checkBox, specialty.specialization_id);
                        SpecialtiesFilterPanel.Children.Add(checkBox);
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка загрузки специальностей: {ex.Message}", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
            }
        }

        private void LoadDefaultFilters()
        {
            foreach (var checkBox in specialtyCheckBoxes.Keys)
                checkBox.IsChecked = true;

            FilterCourse1.IsChecked = true;
            FilterCourse2.IsChecked = true;
            FilterCourse3.IsChecked = true;
            FilterCourse4.IsChecked = true;

            UpdateSelectedSpecialties();
            UpdateSelectedCourses();
        }

        private void LoadHonorBoardData()
        {
            try
            {
                using (var db = new GenioAppEntities())
                {
                    var honorRecords = db.HonorBoard_GetAll();
                    var allStudents = db.Students_GetAll();

                    var studentsDict = allStudents.ToDictionary(s => s.student_id);

                    var filteredRecords = honorRecords.AsEnumerable();

                    if (selectedYear > 0)
                    {
                        filteredRecords = filteredRecords.Where(h => h.placement_date.Year == selectedYear);
                    }

                    if (selectedCourses.Any())
                    {
                        filteredRecords = filteredRecords.Where(h =>
                        {
                            if (studentsDict.TryGetValue(h.student_id, out var student))
                                return selectedCourses.Contains(student.course_number);
                            return false;
                        });
                    }

                    if (selectedSpecialties.Any())
                    {
                        filteredRecords = filteredRecords.Where(h =>
                        {
                            if (studentsDict.TryGetValue(h.student_id, out var student))
                                return selectedSpecialties.Contains(student.specialization_id);
                            return false;
                        });
                    }

                    var sortedRecords = filteredRecords
                        .OrderByDescending(h => h.placement_date)
                        .ThenBy(h =>
                        {
                            if (studentsDict.TryGetValue(h.student_id, out var s)) return s.last_name;
                            return "";
                        })
                        .ThenBy(h =>
                        {
                            if (studentsDict.TryGetValue(h.student_id, out var s)) return s.first_name;
                            return "";
                        })
                        .ThenBy(h =>
                        {
                            if (studentsDict.TryGetValue(h.student_id, out var s)) return s.middle_name;
                            return "";
                        })
                        .ToList();

                    allHonorItems = new List<HonorDisplayItem>();
                    foreach (var record in sortedRecords)
                    {
                        if (studentsDict.TryGetValue(record.student_id, out var student))
                        {
                            allHonorItems.Add(new HonorDisplayItem
                            {
                                FullName = $"{student.last_name} {student.first_name} {student.middle_name}",
                                LastName = student.last_name,
                                FirstName = student.first_name,
                                MiddleName = student.middle_name,
                                Course = student.course_number,
                                Group = student.group_name,
                                AdmissionDate = student.admission_date,
                                GraduationDate = student.graduation_date ?? student.admission_date.AddYears(4),
                                PlacementDate = record.placement_date,
                                StudentId = student.student_id,
                                HonorId = record.honor_id
                            });
                        }
                    }

                    honorItemsView = CollectionViewSource.GetDefaultView(allHonorItems);
                    honorItemsView.Filter = HonorItemFilter;
                    HonorBoardGrid.ItemsSource = null;
                    HonorBoardGrid.ItemsSource = honorItemsView;
                    ExportExcelBtn.IsEnabled = allHonorItems.Any();

                    UpdateEditButtonState();
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка загрузки данных доски почета: {ex.Message}", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
            }
        }

        private void UpdateEditButtonState()
        {
            EditButton.IsEnabled = selectedYear > 0;
        }

        private bool HonorItemFilter(object item)
        {
            if (string.IsNullOrWhiteSpace(currentSearchText) || currentSearchText == "Поиск...")
                return true;

            var honorItem = item as HonorDisplayItem;
            if (honorItem == null) return false;

            string searchLower = currentSearchText.ToLower().Trim();
            var searchWords = searchLower.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var word in searchWords)
            {
                bool wordMatches =
                    honorItem.LastName.ToLower().Contains(word) ||
                    honorItem.FirstName.ToLower().Contains(word) ||
                    honorItem.MiddleName.ToLower().Contains(word) ||
                    honorItem.FullName.ToLower().Contains(word) ||
                    honorItem.Group.ToLower().Contains(word);

                if (!wordMatches) return false;
            }
            return true;
        }

        private void UpdateSelectedSpecialties()
        {
            selectedSpecialties.Clear();
            foreach (var kvp in specialtyCheckBoxes)
            {
                if (kvp.Key.IsChecked == true)
                    selectedSpecialties.Add(kvp.Value);
            }
        }

        private void UpdateSelectedCourses()
        {
            selectedCourses.Clear();
            if (FilterCourse1.IsChecked == true) selectedCourses.Add(1);
            if (FilterCourse2.IsChecked == true) selectedCourses.Add(2);
            if (FilterCourse3.IsChecked == true) selectedCourses.Add(3);
            if (FilterCourse4.IsChecked == true) selectedCourses.Add(4);
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Text == "Поиск...")
            {
                SearchTextBox.Text = "";
                SearchTextBox.Foreground = Brushes.White;
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                SearchTextBox.Text = "Поиск...";
                SearchTextBox.Foreground = Brushes.Gray;
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            currentSearchText = SearchTextBox.Text;
            if (string.IsNullOrWhiteSpace(currentSearchText) || currentSearchText == "Поиск...")
                currentSearchText = "";
            ApplySearchFilter();
        }

        private void SearchTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                ApplySearchFilter();
                Keyboard.ClearFocus();
            }
        }

        private void ApplySearchFilter()
        {
            if (honorItemsView != null)
                honorItemsView.Refresh();
        }

        private void FiltersBtn_Click(object sender, RoutedEventArgs e)
        {
            filtersVisible = !filtersVisible;
            if (filtersVisible)
            {
                FiltersPanel.Visibility = Visibility.Visible;
                FiltersPanel.Margin = new Thickness(20, 0, 20, 15);
                FiltersBtn.Style = (Style)FindResource("FiltersButtonActiveStyle");
            }
            else
            {
                FiltersPanel.Visibility = Visibility.Collapsed;
                FiltersPanel.Margin = new Thickness(20, 0, 20, 15);
                FiltersBtn.Style = (Style)FindResource("FiltersButtonStyle");
            }
        }

        private void ExportExcelBtn_Click(object sender, RoutedEventArgs e)
        {
            var itemsToExport = honorItemsView?.Cast<HonorDisplayItem>().ToList() ?? allHonorItems;
            if (itemsToExport.Count == 0)
            {
                CustomMessageBox.Show("Нет данных для экспорта", "Внимание",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "CSV файлы (*.csv)|*.csv|Excel файлы (*.xlsx)|*.xlsx|Все файлы (*.*)|*.*",
                    FileName = $"Доска_почета_{DateTime.Now:yyyyMMdd_HHmm}.csv",
                    DefaultExt = ".csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    ExportToCSV(saveDialog.FileName, itemsToExport);
                    CustomMessageBox.Show($"Данные успешно экспортированы!", "Успех",
                        CustomMessageBoxButton.OK, CustomMessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка при экспорте: {ex.Message}", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
            }
        }

        private void ExportToCSV(string filePath, List<HonorDisplayItem> items)
        {
            using (var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
            {
                writer.WriteLine("ФИО;Курс;Группа;Дата поступления;Дата окончания;Дата занесения на ДП");
                foreach (var item in items)
                {
                    writer.WriteLine($"\"{item.FullName}\";{item.Course};\"{item.Group}\";" +
                                   $"{item.AdmissionDate:dd.MM.yyyy};{item.GraduationDate:dd.MM.yyyy};" +
                                   $"{item.PlacementDate:dd.MM.yyyy}");
                }
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                var page = new AddHonorPage();
                mainWindow.MainFrame.Navigate(page);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedYear > 0)
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    var page = new AddHonorPage(selectedYear);
                    mainWindow.MainFrame.Navigate(page);
                }
            }
            else
            {
                CustomMessageBox.Show("Выберите конкретный год для редактирования", "Внимание",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Warning);
            }
        }

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var checkBox in specialtyCheckBoxes.Keys)
                checkBox.IsChecked = true;

            FilterCourse1.IsChecked = true;
            FilterCourse2.IsChecked = true;
            FilterCourse3.IsChecked = true;
            FilterCourse4.IsChecked = true;
            YearComboBox.SelectedIndex = 0;
            SearchTextBox.Text = "Поиск...";
            SearchTextBox.Foreground = Brushes.Gray;
            currentSearchText = "";

            UpdateSelectedSpecialties();
            UpdateSelectedCourses();
            selectedYear = 0;
            LoadHonorBoardData();
        }

        private void YearComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (YearComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                selectedYear = selectedItem.Tag != null ? (int)selectedItem.Tag : 0;
                LoadHonorBoardData();
            }
        }

        private void SpecialtyFilter_Changed(object sender, RoutedEventArgs e)
        {
            UpdateSelectedSpecialties();
            LoadHonorBoardData();
        }

        private void CourseFilter_Changed(object sender, RoutedEventArgs e)
        {
            UpdateSelectedCourses();
            LoadHonorBoardData();
        }

        private void SearchTextBox_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (SearchTextBox.Text == "Поиск...")
            {
                SearchTextBox.Text = "";
                SearchTextBox.Foreground = Brushes.White;
                SearchTextBox.Focus();
                e.Handled = true;
            }
        }

        private void HonorBoardGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (HonorBoardGrid.SelectedItem is HonorDisplayItem selectedItem)
            {
                ShowStudentInfoModal(selectedItem.StudentId);
            }
        }

        private void ShowStudentInfoModal(int studentId)
        {
            try
            {
                using (var db = new GenioAppEntities())
                {
                    var student = db.Students_GetById(studentId);
                    if (student == null)
                    {
                        CustomMessageBox.Show("Студент не найден", "Ошибка",
                            CustomMessageBoxButton.OK, CustomMessageBoxIcon.Warning);
                        return;
                    }

                    // Явные цвета для тёмной темы
                    var darkBackground = new SolidColorBrush(Color.FromRgb(38, 38, 38));
                    var lightText = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                    var grayText = new SolidColorBrush(Color.FromRgb(200, 200, 200));
                    var accentColor = new SolidColorBrush(Color.FromRgb(97, 85, 245));

                    var infoWindow = new Window
                    {
                        Title = $"Информация об учащемся: {student.last_name} {student.first_name}",
                        Width = 520,
                        Height = 450,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        ResizeMode = ResizeMode.NoResize,
                        Background = darkBackground,
                        WindowStyle = WindowStyle.SingleBorderWindow,
                        Owner = Application.Current.MainWindow,
                        ShowInTaskbar = false
                    };

                    var mainGrid = new Grid { Margin = new Thickness(25) };
                    mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    // Заголовок
                    var titleText = new TextBlock
                    {
                        Text = $"{student.last_name} {student.first_name} {student.middle_name}",
                        FontSize = 19,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = lightText,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 20),
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center
                    };
                    Grid.SetRow(titleText, 0);
                    mainGrid.Children.Add(titleText);

                    // Данные
                    var dataGrid = new Grid { Margin = new Thickness(5, 0, 5, 20) };
                    dataGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    dataGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    // RowDefinitions для каждой строки данных
                    var labels = new[]
                    {
                "Дата рождения:", "Курс:", "Группа:", "Специальность:",
                "Дата поступления:", "Дата окончания:", "Телефон:", "Дом. телефон:"
            };

                    // RowDefinitions
                    for (int i = 0; i < labels.Length; i++)
                    {
                        dataGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    }

                    var values = new[]
                    {
                student.birth_date.ToString("dd.MM.yyyy"),
                student.course_number.ToString(),
                student.group_name,
                student.Specialization?.spec_name ?? "Не указано",
                student.admission_date.ToString("dd.MM.yyyy"),
                student.graduation_date?.ToString("dd.MM.yyyy") ?? "Не указано",
                student.phone ?? "Не указано",
                student.home_phone ?? "Не указано"
            };

                    for (int i = 0; i < labels.Length; i++)
                    {
                        var label = new TextBlock
                        {
                            Text = labels[i],
                            FontSize = 13,
                            FontWeight = FontWeights.SemiBold,
                            Foreground = lightText,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 20, 8)
                        };
                        Grid.SetRow(label, i);
                        Grid.SetColumn(label, 0);
                        dataGrid.Children.Add(label);

                        var value = new TextBlock
                        {
                            Text = values[i],
                            FontSize = 13,
                            Foreground = grayText,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 0, 8),
                            TextWrapping = TextWrapping.Wrap
                        };
                        Grid.SetRow(value, i);
                        Grid.SetColumn(value, 1);
                        dataGrid.Children.Add(value);
                    }

                    Grid.SetRow(dataGrid, 1);
                    mainGrid.Children.Add(dataGrid);

                    // Кнопка
                    var closeButton = new Button
                    {
                        Content = "Закрыть",
                        Style = (Style)Application.Current.FindResource("AccentButtonStyle"),
                        Width = 130,
                        Height = 38,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Cursor = Cursors.Hand
                    };
                    closeButton.Click += (s, args) => infoWindow.Close();

                    Grid.SetRow(closeButton, 2);
                    mainGrid.Children.Add(closeButton);

                    infoWindow.Content = mainGrid;
                    infoWindow.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка загрузки информации о студенте: {ex.Message}", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
            }
        }
    }
}