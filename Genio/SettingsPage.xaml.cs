using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Genio
{
    public partial class SettingsPage : Page
    {
        private List<Specialization> allSpecializations = new List<Specialization>();
        private List<EventType> allEventTypes = new List<EventType>();
        private Specialization currentEditingSpecialization = null;
        private EventType currentEditingEventType = null;

        public SettingsPage()
        {
            InitializeComponent();
            Loaded += SettingsPage_Loaded;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSpecializationsFromDatabase();
            LoadEventTypesFromDatabase();
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

                    RefreshSpecialtiesList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки специальностей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadEventTypesFromDatabase()
        {
            try
            {
                using (var context = new GenioAppEntities())
                {
                    allEventTypes = context.EventTypes
                        .OrderBy(et => et.type_name)
                        .ToList();

                    RefreshEventTypesList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки видов мероприятий: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshSpecialtiesList()
        {
            SpecialtiesList.Children.Clear();

            foreach (var specialization in allSpecializations)
            {
                CreateSpecialtyItem(specialization);
            }
        }

        private void RefreshEventTypesList()
        {
            EventTypesList.Children.Clear();

            foreach (var eventType in allEventTypes)
            {
                CreateEventTypeItem(eventType);
            }
        }

        private void CreateSpecialtyItem(Specialization specialization)
        {
            var border = new Border();

            if (currentEditingSpecialization != null && currentEditingSpecialization.specialization_id == specialization.specialization_id)
            {
                border.Style = (Style)FindResource("SelectedStudentItemStyle");
            }
            else
            {
                border.Style = (Style)FindResource("UnselectedStudentItemStyle");
            }

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Текстовое поле для редактирования
            TextBox textBox = null;

            if (currentEditingSpecialization != null && currentEditingSpecialization.specialization_id == specialization.specialization_id)
            {
                textBox = new TextBox
                {
                    Text = specialization.spec_name,
                    Style = (Style)FindResource("SearchTextBoxStyle"),
                    Background = Brushes.Transparent,
                    Foreground = (SolidColorBrush)FindResource("LightTextColor"),
                    FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 10, 0),
                    BorderThickness = new Thickness(0)
                };

                Grid.SetColumn(textBox, 0);
                grid.Children.Add(textBox);
            }
            else
            {
                var textBlock = new TextBlock
                {
                    Text = specialization.spec_name,
                    Foreground = (SolidColorBrush)FindResource("LightTextColor"),
                    FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 10, 0)
                };

                Grid.SetColumn(textBlock, 0);
                grid.Children.Add(textBlock);
            }

            // Кнопка сохранения/редактирования
            var editSaveButton = new Button
            {
                Style = (Style)FindResource("StudentSelectionButtonStyle"),
                Width = 27,
                Height = 27,
                Tag = specialization,
                Margin = new Thickness(5, 0, 5, 0)
            };

            var editSaveImage = new Image
            {
                Stretch = Stretch.Uniform,
                Width = 15,
                Height = 15
            };

            if (currentEditingSpecialization != null && currentEditingSpecialization.specialization_id == specialization.specialization_id)
            {
                editSaveImage.Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("pack://application:,,,/Images/saveIcon.png"));
                editSaveButton.ToolTip = "Сохранить изменения";
                editSaveButton.Click += SaveSpecialtyButton_Click;
            }
            else
            {
                editSaveImage.Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("pack://application:,,,/Images/editIcon.png"));
                editSaveButton.ToolTip = "Редактировать";
                editSaveButton.Click += EditSpecialtyButton_Click;
            }

            editSaveButton.Content = editSaveImage;
            Grid.SetColumn(editSaveButton, 1);
            grid.Children.Add(editSaveButton);

            // Кнопка удаления
            var deleteButton = new Button
            {
                Style = (Style)FindResource("StudentSelectionButtonStyle"),
                Width = 27,
                Height = 27,
                Tag = specialization,
                Margin = new Thickness(0, 0, 5, 0)
            };

            var deleteImage = new Image
            {
                Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("pack://application:,,,/Images/iconRemove.png")),
                Stretch = Stretch.Uniform,
                Width = 15,
                Height = 15
            };

            deleteButton.Content = deleteImage;
            deleteButton.ToolTip = "Удалить";
            deleteButton.Click += DeleteSpecialtyButton_Click;
            Grid.SetColumn(deleteButton, 2);
            grid.Children.Add(deleteButton);

            border.Child = grid;
            border.Tag = specialization;

            SpecialtiesList.Children.Add(border);
        }

        private void CreateEventTypeItem(EventType eventType)
        {
            var border = new Border();

            if (currentEditingEventType != null && currentEditingEventType.event_type_id == eventType.event_type_id)
            {
                border.Style = (Style)FindResource("SelectedStudentItemStyle");
            }
            else
            {
                border.Style = (Style)FindResource("UnselectedStudentItemStyle");
            }

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Текстовое поле для редактирования
            TextBox textBox = null;

            if (currentEditingEventType != null && currentEditingEventType.event_type_id == eventType.event_type_id)
            {
                textBox = new TextBox
                {
                    Text = eventType.type_name,
                    Style = (Style)FindResource("SearchTextBoxStyle"),
                    Background = Brushes.Transparent,
                    Foreground = (SolidColorBrush)FindResource("LightTextColor"),
                    FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 10, 0),
                    BorderThickness = new Thickness(0)
                };

                Grid.SetColumn(textBox, 0);
                grid.Children.Add(textBox);
            }
            else
            {
                var textBlock = new TextBlock
                {
                    Text = eventType.type_name,
                    Foreground = (SolidColorBrush)FindResource("LightTextColor"),
                    FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 10, 0)
                };

                Grid.SetColumn(textBlock, 0);
                grid.Children.Add(textBlock);
            }

            // Кнопка сохранения/редактирования
            var editSaveButton = new Button
            {
                Style = (Style)FindResource("StudentSelectionButtonStyle"),
                Width = 27,
                Height = 27,
                Tag = eventType,
                Margin = new Thickness(5, 0, 5, 0)
            };

            var editSaveImage = new Image
            {
                Stretch = Stretch.Uniform,
                Width = 15,
                Height = 15
            };

            if (currentEditingEventType != null && currentEditingEventType.event_type_id == eventType.event_type_id)
            {
                editSaveImage.Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("pack://application:,,,/Images/saveIcon.png"));
                editSaveButton.ToolTip = "Сохранить изменения";
                editSaveButton.Click += SaveEventTypeButton_Click;
            }
            else
            {
                editSaveImage.Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("pack://application:,,,/Images/editIcon.png"));
                editSaveButton.ToolTip = "Редактировать";
                editSaveButton.Click += EditEventTypeButton_Click;
            }

            editSaveButton.Content = editSaveImage;
            Grid.SetColumn(editSaveButton, 1);
            grid.Children.Add(editSaveButton);

            // Кнопка удаления
            var deleteButton = new Button
            {
                Style = (Style)FindResource("StudentSelectionButtonStyle"),
                Width = 27,
                Height = 27,
                Tag = eventType,
                Margin = new Thickness(0, 0, 5, 0)
            };

            var deleteImage = new Image
            {
                Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("pack://application:,,,/Images/iconRemove.png")),
                Stretch = Stretch.Uniform,
                Width = 15,
                Height = 15
            };

            deleteButton.Content = deleteImage;
            deleteButton.ToolTip = "Удалить";
            deleteButton.Click += DeleteEventTypeButton_Click;
            Grid.SetColumn(deleteButton, 2);
            grid.Children.Add(deleteButton);

            border.Child = grid;
            border.Tag = eventType;

            EventTypesList.Children.Add(border);
        }

        private void AddSpecialtyButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SpecialtyTextBox.Text))
            {
                MessageBox.Show("Введите название специальности", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new GenioAppEntities())
                {
                    // Проверяем, не существует ли уже такая специальность
                    bool exists = context.Specializations
                        .Any(s => s.spec_name.ToLower() == SpecialtyTextBox.Text.Trim().ToLower());

                    if (exists)
                    {
                        MessageBox.Show("Такая специальность уже существует", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var newSpecialization = new Specialization
                    {
                        spec_name = SpecialtyTextBox.Text.Trim(),
                        created_date = DateTime.Now
                    };

                    context.Specializations.Add(newSpecialization);
                    context.SaveChanges();

                    // Обновляем список
                    LoadSpecializationsFromDatabase();

                    // Очищаем поле ввода
                    SpecialtyTextBox.Text = "";

                    MessageBox.Show("Специальность добавлена", "Успешно",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления специальности: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddEventTypeButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EventTypeTextBox.Text))
            {
                MessageBox.Show("Введите название вида мероприятия", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new GenioAppEntities())
                {
                    // Проверяем, не существует ли уже такой вид мероприятия
                    bool exists = context.EventTypes
                        .Any(et => et.type_name.ToLower() == EventTypeTextBox.Text.Trim().ToLower());

                    if (exists)
                    {
                        MessageBox.Show("Такой вид мероприятия уже существует", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var newEventType = new EventType
                    {
                        type_name = EventTypeTextBox.Text.Trim(),
                        created_date = DateTime.Now
                    };

                    context.EventTypes.Add(newEventType);
                    context.SaveChanges();

                    // Обновляем список
                    LoadEventTypesFromDatabase();

                    // Очищаем поле ввода
                    EventTypeTextBox.Text = "";

                    MessageBox.Show("Вид мероприятия добавлен", "Успешно",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления вида мероприятия: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditSpecialtyButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag is Specialization specialization)
            {
                // Включаем режим редактирования для выбранной специальности
                currentEditingSpecialization = specialization;
                RefreshSpecialtiesList();
            }
        }

        private void SaveSpecialtyButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag is Specialization specialization && currentEditingSpecialization != null)
            {
                try
                {
                    // Находим текстовое поле в текущем элементе
                    var border = button.Parent as Grid;
                    if (border != null && border.Children[0] is TextBox textBox)
                    {
                        string newName = textBox.Text.Trim();

                        if (string.IsNullOrWhiteSpace(newName))
                        {
                            MessageBox.Show("Название специальности не может быть пустым", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        using (var context = new GenioAppEntities())
                        {
                            // Проверяем, не существует ли уже такая специальность
                            bool exists = context.Specializations
                                .Any(s => s.specialization_id != specialization.specialization_id &&
                                       s.spec_name.ToLower() == newName.ToLower());

                            if (exists)
                            {
                                MessageBox.Show("Такая специальность уже существует", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }

                            var specToUpdate = context.Specializations.Find(specialization.specialization_id);
                            if (specToUpdate != null)
                            {
                                specToUpdate.spec_name = newName;
                                context.SaveChanges();

                                // Выходим из режима редактирования
                                currentEditingSpecialization = null;
                                LoadSpecializationsFromDatabase();

                                MessageBox.Show("Специальность обновлена", "Успешно",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка обновления специальности: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteSpecialtyButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag is Specialization specialization)
            {
                var result = MessageBox.Show($"Вы уверены, что хотите удалить специальность \"{specialization.spec_name}\"?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var context = new GenioAppEntities())
                        {
                            // Проверяем, есть ли связанные студенты
                            bool hasStudents = context.Students
                                .Any(s => s.specialization_id == specialization.specialization_id);

                            if (hasStudents)
                            {
                                MessageBox.Show("Невозможно удалить специальность, так как есть связанные студенты. " +
                                    "Сначала измените специальность у этих студентов.", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            var specToDelete = context.Specializations.Find(specialization.specialization_id);
                            if (specToDelete != null)
                            {
                                context.Specializations.Remove(specToDelete);
                                context.SaveChanges();

                                // Выходим из режима редактирования, если удаляем редактируемый элемент
                                if (currentEditingSpecialization != null &&
                                    currentEditingSpecialization.specialization_id == specialization.specialization_id)
                                {
                                    currentEditingSpecialization = null;
                                }

                                LoadSpecializationsFromDatabase();

                                MessageBox.Show("Специальность удалена", "Успешно",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления специальности: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void EditEventTypeButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag is EventType eventType)
            {
                // Включаем режим редактирования для выбранного вида мероприятия
                currentEditingEventType = eventType;
                RefreshEventTypesList();
            }
        }

        private void SaveEventTypeButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag is EventType eventType && currentEditingEventType != null)
            {
                try
                {
                    // Находим текстовое поле в текущем элементе
                    var border = button.Parent as Grid;
                    if (border != null && border.Children[0] is TextBox textBox)
                    {
                        string newName = textBox.Text.Trim();

                        if (string.IsNullOrWhiteSpace(newName))
                        {
                            MessageBox.Show("Название вида мероприятия не может быть пустым", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        using (var context = new GenioAppEntities())
                        {
                            // Проверяем, не существует ли уже такой вид мероприятия
                            bool exists = context.EventTypes
                                .Any(et => et.event_type_id != eventType.event_type_id &&
                                       et.type_name.ToLower() == newName.ToLower());

                            if (exists)
                            {
                                MessageBox.Show("Такой вид мероприятия уже существует", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }

                            var eventTypeToUpdate = context.EventTypes.Find(eventType.event_type_id);
                            if (eventTypeToUpdate != null)
                            {
                                eventTypeToUpdate.type_name = newName;
                                context.SaveChanges();

                                // Выходим из режима редактирования
                                currentEditingEventType = null;
                                LoadEventTypesFromDatabase();

                                MessageBox.Show("Вид мероприятия обновлен", "Успешно",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка обновления вида мероприятия: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteEventTypeButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag is EventType eventType)
            {
                var result = MessageBox.Show($"Вы уверены, что хотите удалить вид мероприятия \"{eventType.type_name}\"?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var context = new GenioAppEntities())
                        {
                            // Проверяем, есть ли связанные мероприятия
                            bool hasEvents = context.Olimps
                                .Any(o => o.event_type_id == eventType.event_type_id);

                            if (hasEvents)
                            {
                                MessageBox.Show("Невозможно удалить вид мероприятия, так как есть связанные мероприятия. " +
                                    "Сначала измените вид у этих мероприятий.", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            var eventTypeToDelete = context.EventTypes.Find(eventType.event_type_id);
                            if (eventTypeToDelete != null)
                            {
                                context.EventTypes.Remove(eventTypeToDelete);
                                context.SaveChanges();

                                // Выходим из режима редактирования, если удаляем редактируемый элемент
                                if (currentEditingEventType != null &&
                                    currentEditingEventType.event_type_id == eventType.event_type_id)
                                {
                                    currentEditingEventType = null;
                                }

                                LoadEventTypesFromDatabase();

                                MessageBox.Show("Вид мероприятия удален", "Успешно",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления вида мероприятия: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}