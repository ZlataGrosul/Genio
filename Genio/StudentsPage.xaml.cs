using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Genio
{
    public partial class StudentsPage : Page
    {
        private bool filtersVisible = false;
        private bool studentInfoVisible = false;
        private int selectedStudentId = 0;
        private Student selectedStudent = null;
        private List<Student> allStudents = new List<Student>();
        private List<Student> filteredStudents = new List<Student>();
        private List<StudentOlimp> studentEvents = new List<StudentOlimp>();
        private List<Specialization> allSpecializations = new List<Specialization>();
        private string currentSearchText = "";
        private List<int> selectedSpecializationIds = new List<int>();
        private List<int> selectedCourses = new List<int>() { 1, 2, 3, 4 };

        public StudentsPage()
        {
            InitializeComponent();
            Loaded += StudentsPage_Loaded;
        }

        private void StudentsPage_Loaded(object sender, RoutedEventArgs e)
        {
            StudentsModeBtn.IsChecked = true;
            ModeSelector.Margin = new Thickness(2, 0, 0, 0);

            LoadAllStudentsFromDatabase();
            LoadSpecializationsFromDatabase();
            CreateSpecializationCheckBoxes();

            SearchTextBox.TextChanged += SearchTextBox_TextChanged;

            ApplyAllFilters();
        }

        private void LoadAllStudentsFromDatabase()
        {
            try
            {
                using (var context = new GenioAppEntities())
                {
                    allStudents = context.Students
                        .Include("Specialization")
                        .OrderBy(s => s.last_name)
                        .ThenBy(s => s.first_name)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки учащихся: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSpecializationsFromDatabase()
        {
            try
            {
                using (var context = new GenioAppEntities())
                {
                    allSpecializations = context.Specializations
                        .OrderBy(s => s.spec_name)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки специальностей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateSpecializationCheckBoxes()
        {
            SpecializationFilters.Children.Clear();

            foreach (var spec in allSpecializations)
            {
                var checkBox = new CheckBox
                {
                    Content = spec.spec_name,
                    Tag = spec.specialization_id,
                    Margin = new Thickness(0, 0, 0, 8),
                    Style = (Style)FindResource("ReportCheckBoxStyle"),
                    FontSize = 12
                };

                checkBox.Checked += SpecializationCheckBox_Changed;
                checkBox.Unchecked += SpecializationCheckBox_Changed;

                SpecializationFilters.Children.Add(checkBox);
            }
        }

        private void ApplyAllFilters()
        {
            try
            {
                filteredStudents = new List<Student>(allStudents);

                if (!string.IsNullOrWhiteSpace(currentSearchText) && currentSearchText != "Поиск...")
                {
                    filteredStudents = filteredStudents
                        .Where(s => s.last_name.ToLower().Contains(currentSearchText.ToLower()) ||
                                   s.first_name.ToLower().Contains(currentSearchText.ToLower()) ||
                                   s.middle_name.ToLower().Contains(currentSearchText.ToLower()) ||
                                   s.group_name.ToLower().Contains(currentSearchText.ToLower()))
                        .ToList();
                }

                if (selectedSpecializationIds.Count > 0)
                {
                    filteredStudents = filteredStudents
                        .Where(s => s.Specialization != null &&
                                   selectedSpecializationIds.Contains(s.Specialization.specialization_id))
                        .ToList();
                }

                if (selectedCourses.Count > 0 && selectedCourses.Count < 4)
                {
                    filteredStudents = filteredStudents
                        .Where(s => selectedCourses.Contains(s.course_number))
                        .ToList();
                }

                UpdateStudentsList();

                if (filteredStudents.Count > 0)
                {
                    if (selectedStudentId > 0 && filteredStudents.Any(s => s.student_id == selectedStudentId))
                    {
                        SelectStudent(selectedStudentId);
                    }
                    else
                    {
                        SelectStudent(filteredStudents[0].student_id);
                    }
                }
                else
                {
                    EventsList.Children.Clear();
                    selectedStudent = null;
                    selectedStudentId = 0;

                    if (allStudents.Count > 0)
                    {
                        StudentsList.Children.Clear();
                        var messageText = new TextBlock
                        {
                            Text = "Нет учащихся, соответствующих выбранным фильтрам",
                            Foreground = (SolidColorBrush)FindResource("LightTextColor"),
                            FontSize = 14,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            TextAlignment = TextAlignment.Center,
                            Margin = new Thickness(0, 20, 0, 0)
                        };
                        StudentsList.Children.Add(messageText);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка применения фильтров: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStudentsList()
        {
            StudentsList.Children.Clear();

            foreach (var student in filteredStudents)
            {
                CreateStudentItem(student);
            }
        }

        private void CreateStudentItem(Student student)
        {
            var border = new Border();

            if (student.student_id == selectedStudentId)
            {
                border.Background = (SolidColorBrush)FindResource("AccentColor");
            }
            else
            {
                border.Background = (SolidColorBrush)FindResource("CalendarDayHover");
            }

            border.CornerRadius = new CornerRadius(4);
            border.Height = 27;
            border.Margin = new Thickness(0, 0, 0, 7);
            border.Tag = student.student_id;
            border.Cursor = Cursors.Hand;

            border.MouseLeftButtonDown += StudentItem_MouseLeftButtonDown;

            var textBlock = new TextBlock
            {
                Text = $"{student.last_name} {student.first_name} {student.middle_name} • {student.group_name}",
                Foreground = Brushes.White,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            border.Child = textBlock;
            StudentsList.Children.Add(border);
        }

        private void LoadEventsForStudent(int studentId)
        {
            try
            {
                using (var context = new GenioAppEntities())
                {
                    studentEvents = context.StudentOlimps
                        .Include("Olimp")
                        .Include("Olimp.EventType")
                        .Where(so => so.student_id == studentId)
                        .OrderByDescending(so => so.Olimp.olimp_date)
                        .ToList();

                    EventsList.Children.Clear();

                    foreach (var studentEvent in studentEvents)
                    {
                        CreateEventCard(studentEvent);
                    }

                    if (studentEvents.Count == 0)
                    {
                        var messageText = new TextBlock
                        {
                            Text = "Учащийся не участвовал в мероприятиях",
                            Foreground = (SolidColorBrush)FindResource("LightTextColor"),
                            FontSize = 14,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            TextAlignment = TextAlignment.Center,
                            Margin = new Thickness(0, 20, 0, 0)
                        };
                        EventsList.Children.Add(messageText);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки мероприятий: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateEventCard(StudentOlimp studentEvent)
        {
            var border = new Border();
            border.Style = (Style)FindResource("EventCardStyle");
            border.Margin = new Thickness(0, 0, 0, 7);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var nameTextBlock = new TextBlock
            {
                Text = studentEvent.Olimp.olimp_name,
                Style = (Style)FindResource("CardTextStyle"),
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            string resultText = !string.IsNullOrEmpty(studentEvent.result) ? $" • {studentEvent.result}" : "";
            var dateTextBlock = new TextBlock
            {
                Text = $"{studentEvent.Olimp.olimp_date:dd.MM.yyyy}{resultText}",
                Foreground = (SolidColorBrush)FindResource("WindowBackground"),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };

            Grid.SetColumn(nameTextBlock, 0);
            Grid.SetColumn(dateTextBlock, 1);

            grid.Children.Add(nameTextBlock);
            grid.Children.Add(dateTextBlock);

            border.Child = grid;
            EventsList.Children.Add(border);
        }

        private void SelectStudent(int studentId)
        {
            ResetStudentSelection();

            foreach (Border border in StudentsList.Children)
            {
                if (border.Tag != null && border.Tag.ToString() == studentId.ToString())
                {
                    border.Background = (SolidColorBrush)FindResource("AccentColor");
                    LoadSelectedStudentInfo(studentId);
                    LoadEventsForStudent(studentId);
                    break;
                }
            }

            selectedStudentId = studentId;
        }

        private void ResetStudentSelection()
        {
            foreach (Border border in StudentsList.Children)
            {
                border.Background = (SolidColorBrush)FindResource("CalendarDayHover");
            }
        }

        private void LoadSelectedStudentInfo(int studentId)
        {
            try
            {
                using (var context = new GenioAppEntities())
                {
                    selectedStudent = context.Students
                        .Include("Specialization")
                        .FirstOrDefault(s => s.student_id == studentId);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки информации об учащемся: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadStudentInfoToForm(Student student)
        {
            StudentNameValue.Text = $"{student.last_name} {student.first_name} {student.middle_name}";
            StudentBirthDateValue.Text = student.birth_date.ToString("dd.MM.yyyy");
            StudentCourseValue.Text = student.course_number.ToString();
            StudentGroupValue.Text = student.group_name;
            StudentSpecializationValue.Text = student.Specialization?.spec_name ?? "Не указано";
            StudentAdmissionDateValue.Text = student.admission_date.ToString("dd.MM.yyyy");
            StudentGraduationDateValue.Text = student.graduation_date?.ToString("dd.MM.yyyy") ?? "Не указано";
            StudentPhoneValue.Text = student.phone ?? "Не указано";
            StudentHomePhoneValue.Text = student.home_phone ?? "Не указано";
        }

        private void StudentItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border != null && border.Tag != null)
            {
                int.TryParse(border.Tag.ToString(), out int studentId);

                if (e.ClickCount == 2)
                {
                    selectedStudentId = studentId;
                    ShowStudentInfo();
                }
                else
                {
                    SelectStudent(studentId);
                }
            }
        }

        private void ShowStudentInfo()
        {
            if (selectedStudent == null)
            {
                MessageBox.Show("Выберите учащегося", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            RightSectionTitle.Text = "Об учащемся";
            EventsSection.Visibility = Visibility.Collapsed;
            StudentInfoSection.Visibility = Visibility.Visible;
            studentInfoVisible = true;

            LoadStudentInfoToForm(selectedStudent);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            RightSectionTitle.Text = "Мероприятия учащегося";
            StudentInfoSection.Visibility = Visibility.Collapsed;
            EventsSection.Visibility = Visibility.Visible;
            studentInfoVisible = false;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // Переход на страницу добавления учащегося с указанием страницы-источника
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                // Передаем информацию о том, что переходим с StudentsPage
                var page = new AddEditStudPage(false, "StudentsPage");
                mainWindow.MainFrame.Navigate(page);
                mainWindow.LockMenu(true);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedStudentId > 0)
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    // Переход на страницу редактирования учащегося с указанием страницы-источника и ID студента
                    var page = new AddEditStudPage(true, "StudentsPage", selectedStudentId);
                    mainWindow.MainFrame.Navigate(page);
                    mainWindow.LockMenu(true);
                }
            }
            else
            {
                MessageBox.Show("Выберите учащегося для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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
                SearchTextBox.Foreground = new SolidColorBrush(Color.FromArgb(255, 140, 140, 140));
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            currentSearchText = SearchTextBox.Text;
            ApplyAllFilters();
        }

        private void SpecializationCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null && checkBox.Tag is int specId)
            {
                if (checkBox.IsChecked == true)
                {
                    if (!selectedSpecializationIds.Contains(specId))
                        selectedSpecializationIds.Add(specId);
                }
                else
                {
                    selectedSpecializationIds.Remove(specId);
                }

                ApplyAllFilters();
            }
        }

        private void CourseCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                int courseNumber = 0;
                if (checkBox.Name == "Course1") courseNumber = 1;
                else if (checkBox.Name == "Course2") courseNumber = 2;
                else if (checkBox.Name == "Course3") courseNumber = 3;
                else if (checkBox.Name == "Course4") courseNumber = 4;

                if (checkBox.IsChecked == true)
                {
                    if (!selectedCourses.Contains(courseNumber))
                        selectedCourses.Add(courseNumber);
                }
                else
                {
                    selectedCourses.Remove(courseNumber);
                }

                ApplyAllFilters();
            }
        }

        private void FiltersBtn_Click(object sender, RoutedEventArgs e)
        {
            filtersVisible = !filtersVisible;

            if (filtersVisible)
            {
                FiltersPanel.Visibility = Visibility.Visible;
                EventsSection.Margin = new Thickness(10, 0, 20, 245);
                EventsSection.Height = 260;
                EventsSection.VerticalAlignment = VerticalAlignment.Top;

                FiltersBtn.Style = (Style)FindResource("FiltersButtonActiveStyle");

                if (studentInfoVisible)
                {
                    studentInfoVisible = false;
                    StudentInfoSection.Visibility = Visibility.Collapsed;
                    EventsSection.Visibility = Visibility.Visible;
                }
            }
            else
            {
                FiltersPanel.Visibility = Visibility.Collapsed;
                EventsSection.Margin = new Thickness(10, 0, 20, 15);
                EventsSection.Height = Double.NaN;
                EventsSection.VerticalAlignment = VerticalAlignment.Stretch;

                FiltersBtn.Style = (Style)FindResource("FiltersButtonStyle");
            }
        }

        private void ViewMode_Checked(object sender, RoutedEventArgs e)
        {
            var button = sender as RadioButton;
            if (button != null)
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    if (button.Name == "StudentsModeBtn")
                    {
                        mainWindow.LoadPage("Students");
                    }
                    else if (button.Name == "EventsModeBtn")
                    {
                        mainWindow.LoadPage("Olympiads");
                    }
                }
            }
        }

        private void ReportBtn_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.LoadPage("Reports");
            }
        }
    }
}