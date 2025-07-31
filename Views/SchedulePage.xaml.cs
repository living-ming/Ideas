using IDEAs.Models;
using IDEAs.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace IDEAs.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// 
    /// </summary>
    public sealed partial class SchedulePage : Page, INotifyPropertyChanged
    {
        private ObservableCollection<Schedule_Event> _scheduleInstance;
        public ObservableCollection<Schedule_Event> ScheduleInstance
        {
            get => _scheduleInstance;
            set
            {
                if (_scheduleInstance != value)
                {
                    _scheduleInstance = value;
                    OnPropertyChanged(nameof(ScheduleInstance));
                }
            }
        }

        public ObservableCollection<string> Schedule_Categories { get; set; } = new ObservableCollection<string>();

        public Schedule AllSchedule { get; set; }
        private string currentCategory;

        private DataService _dataService;

        public SchedulePage()
        {
            this.InitializeComponent();
            _dataService = ((App)Application.Current).DataService;
            this.DataContext = this;
            Loaded += SchedulePage_Loaded;
        }

        private async void SchedulePage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadScheduleAsync();
        }

        private async Task LoadScheduleAsync()
        {
            // ��������
            AllSchedule = await _dataService.LoadSchedule();

            // ��ʼ���¼�����
            ScheduleInstance = new ObservableCollection<Schedule_Event>(AllSchedule.Schedules);
            ScheduleInstance.CollectionChanged += ScheduleInstance_CollectionChanged;

            // �����¼����Ա仯
            foreach (var scheduleEvent in ScheduleInstance)
            {
                scheduleEvent.PropertyChanged += ScheduleEvent_PropertyChanged;
            }

            // ��ʼ����𼯺�
            Schedule_Categories = new ObservableCollection<string>(AllSchedule.Categories);
            Schedule_Categories.CollectionChanged += ScheduleCategories_CollectionChanged;

            ApplyFilter();
            GenerateCategoryButtons();
        }

        private void ScheduleInstance_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // ����������
            if (e.NewItems != null)
            {
                foreach (Schedule_Event newItem in e.NewItems)
                {
                    newItem.PropertyChanged += ScheduleEvent_PropertyChanged;
                }
            }

            // �����Ƴ���
            if (e.OldItems != null)
            {
                foreach (Schedule_Event oldItem in e.OldItems)
                {
                    oldItem.PropertyChanged -= ScheduleEvent_PropertyChanged;
                }
            }

            // ��������
            SaveData();
        }

        private void ScheduleEvent_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // ��������
            SaveData();
        }

        private void ScheduleCategories_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // ��������
            SaveData();
        }

        private void SaveData()
        {

            // ���浽�ļ�
            _dataService.SaveToFile(AllSchedule);
        }

        private void ApplyFilter(string filterOption = null, string category = null)
        {
            // �����������ڣ�ȥ��ʱ�䲿�֣�
            var today = DateTimeOffset.Now.Date;

            IEnumerable<Schedule_Event> query = AllSchedule.Schedules;

            // Ӧ��ɸѡ����
            if (!string.IsNullOrEmpty(filterOption))
            {
                switch (filterOption)
                {
                    case "�����¼�":
                        // ����¼��Ŀ�ʼ�ͽ���ʱ�䶼��ֵ��������ʱ��ΰ������죬����Ϊ�����¼�
                        query = query.Where(e =>
                            e.StartTime.HasValue && e.EndTime.HasValue &&
                            e.StartTime.Value.Date <= today && e.EndTime.Value.Date >= today);
                        break;
                    case "��Ҫ":
                        query = query.Where(e => e.IsImportant);
                        break;
                    case "δ���":
                        query = query.Where(e => !e.IsCompleted);
                        break;
                    case "�����":
                        query = query.Where(e => e.IsCompleted);
                        break;
                    case "����":
                        if (!string.IsNullOrEmpty(category))
                        {
                            query = query.Where(e => e.Category != null && e.Category == category);
                        }
                        break;
                    default:
                        // ��ɸѡ
                        break;
                }
            }

            // ����˳��˵����
            // 1. ��Ҫ�� (��Ҫ����ǰ)
            // 2. �Ƿ���������ա� (�����������¼��Ŀ�ʼ�ͽ���֮�䣬��Ϊ�����¼�)
            // 3. �޸�ʱ�� (����޸ĵ���ǰ)
            query = query
                .OrderByDescending(e => e.IsImportant)
                .ThenByDescending(e =>
                    e.StartTime.HasValue && e.EndTime.HasValue
                    ? (e.StartTime.Value.Date <= today && e.EndTime.Value.Date >= today)
                    : false)
                .ThenByDescending(e => e.LastModifiedTime);

            // �����¼����ϣ����� ScheduleInstance �Ǹ� ObservableCollection �����Ƶļ��ϣ�
            ScheduleInstance.Clear();
            foreach (var evt in query)
            {
                ScheduleInstance.Add(evt);
            }
        }

        private void GenerateCategoryButtons()
        {
            // ������еķ��ఴť
            FilterButtonPanel.Children.Clear();

            // ���Ĭ��ɸѡ��ť���硰ȫ�������������¼����ȣ�
            // ����Ը�����Ҫ��Ӹ���Ĭ�ϰ�ť
            var allButton = new Button
            {
                Content = "ȫ��",
                Tag = "ȫ��",
                Margin = new Thickness(5, 0, 0, 0)
            };
            allButton.Click += FilterButton_Click;
            FilterButtonPanel.Children.Add(allButton);

            var todayButton = new Button
            {
                Content = "�����¼�",
                Tag = "�����¼�",
                Margin = new Thickness(5, 0, 0, 0)
            };
            todayButton.Click += FilterButton_Click;
            FilterButtonPanel.Children.Add(todayButton);
            var uncompletedButton = new Button
            {
                Content = "δ���",
                Tag = "δ���",
                Margin = new Thickness(5, 0, 0, 0)
            };
            uncompletedButton.Click += FilterButton_Click;
            FilterButtonPanel.Children.Add(uncompletedButton);
            var completedButton = new Button
            {
                Content = "�����",
                Tag = "�����",
                Margin = new Thickness(5, 0, 0, 0)
            };
            completedButton.Click += FilterButton_Click;
            FilterButtonPanel.Children.Add(completedButton);


            var importantButton = new Button
            {
                Content = "��Ҫ",
                Tag = "��Ҫ",
                Margin = new Thickness(5, 0, 0, 0)
            };
            importantButton.Click += FilterButton_Click;
            FilterButtonPanel.Children.Add(importantButton);

            // ��̬��ӷ��ఴť
            // ��̬��ӷ��ఴť
            foreach (var categoryName in Schedule_Categories)
            {
                var button = new Button
                {
                    Content = categoryName,
                    Tag = categoryName,
                    Margin = new Thickness(5, 0, 0, 0),
                    // ������Ҫ���ð�ť����ʽ
                };
                button.Click += CategoryButton_Click;

                // ����Ҽ�����¼�������
                button.RightTapped += CategoryButton_RightClick;

                FilterButtonPanel.Children.Add(button);
            }
        }
        private void CategoryButton_RightClick(object sender, RightTappedRoutedEventArgs e)
        {
            var button = sender as Button;
            var categoryName = button.Tag as string;

            // �����˵�
            MenuFlyout menuFlyout = new MenuFlyout();

            // ��������ɾ�����ࡱ�˵���
            MenuFlyoutItem deleteCategoryItem = new MenuFlyoutItem
            {
                Text = "��ɾ������"
            };
            deleteCategoryItem.Click += (s, ev) => DeleteCategory(categoryName, false);

            // ������ɾ��ȫ�����˵���
            MenuFlyoutItem deleteAllItem = new MenuFlyoutItem
            {
                Text = "ɾ��ȫ��"
            };
            deleteAllItem.Click += (s, ev) => DeleteCategory(categoryName, true);

            // ���˵�����ӵ��˵�
            menuFlyout.Items.Add(deleteCategoryItem);
            menuFlyout.Items.Add(deleteAllItem);

            // ���������λ����ʾ�˵�
            menuFlyout.ShowAt(button, e.GetPosition(button));

            // ��ֹ�¼���������
            e.Handled = true;
        }
        private async void DeleteCategory(string categoryName, bool deleteEvents)
        {
            // ����ȷ�϶Ի���
            string message = deleteEvents
                ? $"ȷ��Ҫɾ�����ࡰ{categoryName}�����������¼���"
                : $"ȷ��Ҫɾ�����ࡰ{categoryName}���𣿣��÷����µ��¼���������";

            // ���� ContentDialog
            ContentDialog confirmDialog = new ContentDialog
            {
                Title = "ȷ��ɾ��",
                Content = message,
                PrimaryButtonText = "ȷ��",
                CloseButtonText = "ȡ��",
                DefaultButton = ContentDialogButton.Close
            };

            // ���� XamlRoot ����
            confirmDialog.XamlRoot = this.Content.XamlRoot;

            // ��ʾ�Ի���
            var result = await confirmDialog.ShowAsync();


            if (result == ContentDialogResult.Primary)
            {
                // �ӷ����б����Ƴ��÷���
                Schedule_Categories.Remove(categoryName);
                AllSchedule.Categories.Remove(categoryName);
                if (deleteEvents)
                {
                    // ɾ���÷����µ������¼�
                    for (int i = AllSchedule.Schedules.Count - 1; i >= 0; i--)
                    {
                        if (AllSchedule.Schedules[i].Category == categoryName)
                        {
                            AllSchedule.Schedules.RemoveAt(i);
                        }
                    }
                }
                else
                {
                    // �����¼��������¼��ķ�����Ϊ�ջ�Ĭ�Ϸ���
                    foreach (var evt in AllSchedule.Schedules)
                    {
                        if (evt.Category == categoryName)
                        {
                            evt.Category = null; // ����ָ��Ϊ����Ĭ�Ϸ��࣬���� "δ����"
                        }
                    }
                }

                // �������ɷ��ఴť
                GenerateCategoryButtons();

                // ����Ӧ��ɸѡ�������¼��б�
                ApplyFilter();
            }
        }

        private void CategoryButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var categoryName = button.Tag as string;
            if (categoryName != null)
            {
                currentCategory = categoryName;
                ApplyFilter("����", categoryName);
            }
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            string filterOption = button.Tag.ToString();
            currentCategory = null;  // ���õ�ǰ����
            ApplyFilter(filterOption);
        }

        private void Add_Schedule_Click(object sender, RoutedEventArgs e)
        {
            var now = DateTimeOffset.Now;
            var newEvent = new Schedule_Event
            {
                Title = "New Schedule",  // ��ͨ���Ի����ȡ�û�����
                                         // ����ʼʱ����Ϊ��������ڣ�ʱ��Ϊ 0:00
                StartTime = new DateTimeOffset(now.Date, now.Offset),
                EndTime = new DateTimeOffset(now.Date, now.Offset),
                LastModifiedTime = now,
                IsImportant = false,
                IsCompleted = false
            };
            // ����е�ǰ���࣬�����¼��ķ���
            if (currentCategory != null)
            {
                newEvent.Category = currentCategory;
            }

            // �����¼���ӵ������¼�������
            AllSchedule.Schedules.Add(newEvent);
            newEvent.PropertyChanged += ScheduleEvent_PropertyChanged;

            // ������º���ճ̱�
            SaveData();

            // ����Ӧ�õ�ǰ��ɸѡ������
            if (currentCategory != null)
            {
                ApplyFilter("����", currentCategory);
            }
            else
            {
                ApplyFilter();
            }
        }

        private async void Add_Category_Click(object sender, RoutedEventArgs e)
        {
            // ����Ի���
            TextBox inputTextBox = new TextBox()
            {
                PlaceholderText = "�������µ��������"
            };

            ContentDialog dialog = new ContentDialog()
            {
                Title = "��������",
                Content = inputTextBox,
                PrimaryButtonText = "ȷ��",
                CloseButtonText = "ȡ��",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                string userInput = inputTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(userInput))
                {
                    if (Schedule_Categories.Contains(userInput))
                    {
                        var duplicateDialog = new ContentDialog()
                        {
                            Title = "����Ѵ���",
                            Content = $"��� \"{userInput}\" �Ѵ��ڣ��������������ơ�",
                            CloseButtonText = "ȷ��",
                            XamlRoot = this.Content.XamlRoot
                        };
                        await duplicateDialog.ShowAsync();
                    }
                    else
                    {
                        Schedule_Categories.Add(userInput);
                        AllSchedule.Categories.Add(userInput);

                        // ��������
                        SaveData();

                        // �������ɷ��ఴť
                        GenerateCategoryButtons();
                    }
                }
                else
                {
                    var emptyInputDialog = new ContentDialog()
                    {
                        Title = "������Ч",
                        Content = "������Ʋ���Ϊ�գ����������롣",
                        CloseButtonText = "ȷ��",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await emptyInputDialog.ShowAsync();
                }
            }
        }
        private void Event_IsImportant_Click(object sender, RoutedEventArgs e)
        {
            var eventData = (sender as FrameworkElement).DataContext as Schedule_Event;
            if (eventData != null)
            {
                eventData.IsImportant = !eventData.IsImportant;
            }
        }
        private void Event_Add_Category_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var flyout = new MenuFlyout();

                // ��ȡ��ǰ�¼�
                var eventData = button.DataContext as Schedule_Event;

                // ��ӷ���˵���
                foreach (var categoryName in Schedule_Categories)
                {
                    var menuItem = new MenuFlyoutItem
                    {
                        Text = categoryName,
                        DataContext = eventData
                    };
                    menuItem.Click += CategoryMenuItem_Click;
                    flyout.Items.Add(menuItem);
                }

                // ��ӷָ���
                flyout.Items.Add(new MenuFlyoutSeparator());

                // ��ӡ��Ƴ����ࡱ�˵���
                var removeCategoryItem = new MenuFlyoutItem
                {
                    Text = "�Ƴ�����",
                    DataContext = eventData
                };
                removeCategoryItem.Click += RemoveCategoryMenuItem_Click;
                flyout.Items.Add(removeCategoryItem);

                // ��ӡ�����·��ࡱ�˵���
                var addNewCategoryItem = new MenuFlyoutItem
                {
                    Text = "����·���",
                    DataContext = eventData
                };
                addNewCategoryItem.Click += AddNewCategoryMenuItem_Click;
                flyout.Items.Add(addNewCategoryItem);

                // ��ʾ MenuFlyout
                flyout.ShowAt(button);
            }
        }
        private void RemoveCategoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuFlyoutItem;
            if (menuItem != null)
            {
                // ��ȡ��ǰ�¼�
                var eventData = menuItem.DataContext as Schedule_Event;
                if (eventData != null)
                {
                    eventData.Category = null; // ������Ϊ string.Empty

                    // ��������
                    SaveData();
                }
            }
        }

        private async void AddNewCategoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // ����һ������Ի���
            TextBox inputTextBox = new TextBox()
            {
                PlaceholderText = "�������µ��������"
            };

            ContentDialog dialog = new ContentDialog()
            {
                Title = "��������",
                Content = inputTextBox,
                PrimaryButtonText = "ȷ��",
                CloseButtonText = "ȡ��",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                string userInput = inputTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(userInput))
                {
                    if (Schedule_Categories.Contains(userInput))
                    {
                        var duplicateDialog = new ContentDialog()
                        {
                            Title = "����Ѵ���",
                            Content = $"��� \"{userInput}\" �Ѵ��ڣ��������������ơ�",
                            CloseButtonText = "ȷ��",
                            XamlRoot = this.Content.XamlRoot
                        };
                        await duplicateDialog.ShowAsync();
                    }
                    else
                    {
                        Schedule_Categories.Add(userInput);
                        // ����� AllSchedule.Categories ������������ͬ������
                        AllSchedule.Categories.Add(userInput);

                        // ��������
                        SaveData();

                        // ���·����������ǰ�¼�
                        // ��ȡ��ǰ�¼�
                        var menuItem = sender as MenuFlyoutItem;
                        var eventData = menuItem?.DataContext as Schedule_Event;
                        if (eventData != null)
                        {
                            eventData.Category = userInput;

                            // ��������
                            SaveData();
                        }
                        GenerateCategoryButtons();

                    }
                }
                else
                {
                    var emptyInputDialog = new ContentDialog()
                    {
                        Title = "������Ч",
                        Content = "������Ʋ���Ϊ�գ����������롣",
                        CloseButtonText = "ȷ��",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await emptyInputDialog.ShowAsync();
                }
            }
        }

        private void CategoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuFlyoutItem;
            if (menuItem != null)
            {
                var selectedCategory = menuItem.Text;

                // ��ȡ��ǰ�¼�
                var eventData = menuItem.DataContext as Schedule_Event;
                if (eventData != null)
                {
                    eventData.Category = selectedCategory;

                    // ��������
                    SaveData();
                }
            }
        }

        private void Event_delete_Click(object sender, RoutedEventArgs e)
        {
            var eventData = (sender as FrameworkElement).DataContext as Schedule_Event;
            if (eventData != null)
            {
                AllSchedule.Schedules.Remove(eventData);
                ScheduleInstance.Remove(eventData);
                SaveData();
                // ��������Ӧ�ù�������GridView �Ѿ��󶨵� ScheduleInstance
            }
        }

        // INotifyPropertyChanged ʵ��
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // ���� StartTime �ı�־λ
        private bool _isUpdatingStartTime = false;

        // ���� EndTime �ı�־λ
        private bool _isUpdatingEndTime = false;

        /// <summary>
        /// ���� StartTime��newDate �� newTime Ϊ null ��ʾ��Ӧ���ֲ��䡣
        /// </summary>
        private void UpdateStartTime(Schedule_Event scheduleEvent, DateTimeOffset? newDate, TimeSpan? newTime)
        {
            if (_isUpdatingStartTime) return;  // ��ֹѭ������

            try
            {
                _isUpdatingStartTime = true;
                // ȡ�õ�ǰֵ�����Ϊ����ʹ�õ�ǰʱ����Ϊ����
                DateTimeOffset current = scheduleEvent.StartTime ?? DateTimeOffset.Now;

                // ��������µ����ڣ���ʹ���µ��ꡢ�¡��գ���������ǰ���ڲ���
                int year = newDate?.Year ?? current.Year;
                int month = newDate?.Month ?? current.Month;
                int day = newDate?.Day ?? current.Day;

                // ��������µ�ʱ�䣬��ʹ���µ�ʱ���֡��룻��������ǰʱ�䲿��
                int hour = newTime.HasValue ? newTime.Value.Hours : current.Hour;
                int minute = newTime.HasValue ? newTime.Value.Minutes : current.Minute;
                int second = newTime.HasValue ? newTime.Value.Seconds : current.Second;

                // �����µ� DateTimeOffset��������ԭ�е�ʱ��ƫ��
                DateTimeOffset newStartTime = new DateTimeOffset(year, month, day, hour, minute, second, current.Offset);

                // ֻ�е���ֵ�����仯ʱ�Ž��и�ֵ����
                if (!scheduleEvent.StartTime.HasValue || !scheduleEvent.StartTime.Value.Equals(newStartTime))
                {
                    scheduleEvent.StartTime = newStartTime;
                }
            }
            finally
            {
                _isUpdatingStartTime = false;
            }
        }

        /// <summary>
        /// ���� EndTime��newDate �� newTime Ϊ null ��ʾ��Ӧ���ֲ��䡣
        /// </summary>
        private void UpdateEndTime(Schedule_Event scheduleEvent, DateTimeOffset? newDate, TimeSpan? newTime)
        {
            if (_isUpdatingEndTime) return;  // ��ֹѭ������

            try
            {
                _isUpdatingEndTime = true;
                DateTimeOffset current = scheduleEvent.EndTime ?? DateTimeOffset.Now;

                int year = newDate?.Year ?? current.Year;
                int month = newDate?.Month ?? current.Month;
                int day = newDate?.Day ?? current.Day;

                int hour = newTime.HasValue ? newTime.Value.Hours : current.Hour;
                int minute = newTime.HasValue ? newTime.Value.Minutes : current.Minute;
                int second = newTime.HasValue ? newTime.Value.Seconds : current.Second;

                DateTimeOffset newEndTime = new DateTimeOffset(year, month, day, hour, minute, second, current.Offset);

                if (!scheduleEvent.EndTime.HasValue || !scheduleEvent.EndTime.Value.Equals(newEndTime))
                {
                    scheduleEvent.EndTime = newEndTime;
                }
            }
            finally
            {
                _isUpdatingEndTime = false;
            }
        }

        /// <summary>
        /// CalendarDatePicker �� DateChanged �¼������� StartTime �����ڲ���
        /// </summary>
        private void StartTime_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (sender.DataContext is Schedule_Event scheduleEvent && sender.Date.HasValue)
            {
                // �����������ڣ�ʱ�䲿�ֱ��ֲ��䣨���� null��
                UpdateStartTime(scheduleEvent, sender.Date, null);
            }
        }

        /// <summary>
        /// TimePicker �� TimeChanged �¼������� StartTime ��ʱ�䲿��
        /// </summary>
        private void StartTimePicker_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
        {
            if (sender is TimePicker timePicker && timePicker.DataContext is Schedule_Event scheduleEvent)
            {
                // ��������ʱ�䣬���ڲ��ֱ��ֲ���
                UpdateStartTime(scheduleEvent, null, e.NewTime);
            }
        }

        /// <summary>
        /// CalendarDatePicker �� DateChanged �¼������� EndTime �����ڲ���
        /// </summary>
        private void EndTime_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (sender.DataContext is Schedule_Event scheduleEvent && sender.Date.HasValue)
            {
                UpdateEndTime(scheduleEvent, sender.Date, null);
            }
        }

        /// <summary>
        /// TimePicker �� TimeChanged �¼������� EndTime ��ʱ�䲿��
        /// </summary>
        private void EndTimePicker_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
        {
            if (sender is TimePicker timePicker && timePicker.DataContext is Schedule_Event scheduleEvent)
            {
                UpdateEndTime(scheduleEvent, null, e.NewTime);
            }
        }
    }
}
