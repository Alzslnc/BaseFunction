using Autodesk.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using AppCore = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using AppSystemVariableChangedEventArgs = Autodesk.AutoCAD.ApplicationServices.SystemVariableChangedEventArgs;


//Использование в плагинах

//public class ExampleRibbon : IExtensionApplication
//{
//    public void Initialize()
//    {
//        StartEvents startEvents = new StartEvents();

//        startEvents.Buttons.Add(new Button("Панель", "Вкладка", new List<ButtonCommand>
//            {
//                //кнопка
//                new ButtonCommand("Команда1", "Название кнопки1",
//                "Описание"),
//                //если в одной кнопке несколько команд то добавлять их в список
//                //new ButtonCommand("Команда2", "Название кнопки2",
//                //"Описание"),
//            }
//            ));

//        startEvents.Initialize();
//    }
//    public void Terminate() { }
//}

namespace BaseFunction
{
    internal class StartEvents
    {
        private bool Initialized { get; set; } = false;
        private bool NeedUpdRibbonDetected { get; set; } = false;        
        public List<Button> Buttons { get; set; } = new List<Button>();
        /// <summary>
        /// Инициализация
        /// </summary>
        public void Initialize()
        {
            if (Buttons.Count == 0) return;
            if (!Initialized)
            {
                Initialized = true;

                AppCore.Idle += Application_Idle_RibbonUpdate;
                AppCore.SystemVariableChanged += App_SysVarChanged_RibbonUpdate;
            }
        }
        private void App_SysVarChanged_RibbonUpdate (object sender, AppSystemVariableChangedEventArgs e)
        {
            if (!NeedUpdRibbonDetected
                && 
                (e.Name.Equals("WSCURRENT",
                StringComparison.OrdinalIgnoreCase)
                || e.Name.Equals("RIBBONSTATE",
                StringComparison.OrdinalIgnoreCase)))
            {
                NeedUpdRibbonDetected = true;
                AppCore.Idle += Application_Idle_RibbonUpdate;
            }
        }
        private void Application_Idle_RibbonUpdate(object sender, EventArgs e)
        {
            RibbonControl ribbon = ComponentManager.Ribbon;
            if (ribbon != null)
            {
                AppCore.Idle -= Application_Idle_RibbonUpdate;
                NeedUpdRibbonDetected = false;
                CreateRibbonTab(ribbon);
            }
        }       
        private void CreateRibbonTab(RibbonControl ribCntrl)
        {
            try
            {             
                foreach (Button buttonData in Buttons)
                {
                    if (string.IsNullOrEmpty(buttonData.RibbonTabId) ||
                        string.IsNullOrEmpty(buttonData.RibbonTabName) ||
                        string.IsNullOrEmpty(buttonData.RibbonPanelName) ||
                        buttonData.ButtonCommands.Count == 0) continue;

                    RibbonTab ribTab = null;
                    // добавляем свою вкладку
                    foreach (RibbonTab tab in ribCntrl.Tabs)
                    {
                        if (tab.Id.Equals(buttonData.RibbonTabId) & tab.Title.Equals(buttonData.RibbonTabName))
                        {
                            ribTab = tab;
                            break;
                        }
                    }
                    if (ribTab == null)
                    {
                        ribTab = new RibbonTab
                        {
                            Title = buttonData.RibbonTabName, // Заголовок вкладки
                            Id = buttonData.RibbonTabId // Идентификатор вкладки
                        };
                        ribCntrl.Tabs.Add(ribTab); // Добавляем вкладку в ленту
                    }
                    // добавляем содержимое в свою вкладку (одну панель)
                    AddExampleContent(ribTab, buttonData);
                    // Делаем вкладку активной (не желательно, ибо неудобно)
                    //ribTab.IsActive = true;
                    // Обновляем ленту (если делаете вкладку активной, то необязательно)
                    ribCntrl.UpdateLayout();
                }
            }
            catch { }            
        }
        /// <summary>
        /// Строим панели и кнопки если требуется
        /// </summary>
        private void AddExampleContent(RibbonTab ribTab, Button buttonData)
        {
            RibbonPanel ribPanel = null;       
            foreach (RibbonPanel panel in ribTab.Panels)
            {
                if (panel.Source.Title.Equals(buttonData.RibbonPanelName))
                {
                    ribPanel = panel;
                    break;
                }
            }
            if (ribPanel == null)
            {
                RibbonPanelSource ribSourcePanel = new RibbonPanelSource
                {
                    Title = buttonData.RibbonPanelName
                };
                ribPanel = new RibbonPanel
                {
                    Source = ribSourcePanel
                };
                ribTab.Panels.Add(ribPanel);
            }

            foreach (RibbonItem item in ribPanel.Source.Items)
            {
                if (item is RibbonSplitButton splitButton)
                {
                    if (buttonData.ButtonCommands.Count > 1 && splitButton.Text.ToString() == buttonData.ButtonCommands[0].Name) return;
                }
                else if (item is RibbonButton button)
                { 
                    if (buttonData.ButtonCommands.Count == 1 && button.CommandParameter.ToString() == buttonData.ButtonCommands[0].Command) return;                
                }
            }

            if (buttonData.ButtonCommands.Count == 1) ribPanel.Source.Items.Add(CreateButton(buttonData.ButtonCommands[0]));
            else if (buttonData.ButtonCommands.Count > 1) ribPanel.Source.Items.Add(CreateSplitButton(buttonData.ButtonCommands));

            if (ribPanel.Source.Items.Count == 5) ribPanel.Source.Items.Add(new RibbonPanelBreak());
            else ribPanel.Source.Items.Add(new RibbonRowBreak());
        }
        /// <summary>
        /// создание кнопки
        /// </summary>
        private RibbonButton CreateButton(ButtonCommand buttonTextData)
        {
            RibbonToolTip tt = new RibbonToolTip
            {
                IsHelpEnabled = false,
                Title = buttonTextData.Name,
                Command = buttonTextData.Command,
                Content = buttonTextData.Content
            };
            RibbonButton ribBtn = new RibbonButton
            {
                CommandParameter = buttonTextData.Command,
                Text = buttonTextData.Name,
                Name = buttonTextData.Name,
                CommandHandler = new RibbonCommandHandler(),
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Size = RibbonItemSize.Standard,
                ShowImage = false,
                ShowText = true,
                ToolTip = tt
            };
            return ribBtn;
        }
        /// <summary>
        /// создание общей кнопки
        /// </summary>
        private RibbonSplitButton CreateSplitButton(List<ButtonCommand> buttonTextDatas)
        {
            RibbonSplitButton risSplitBtn = new RibbonSplitButton
            {
                Text = buttonTextDatas[0].Name,
                Orientation = System.Windows.Controls.Orientation.Vertical,
                Size = RibbonItemSize.Standard,
                ShowImage = false,
                ShowText = true,
                ListButtonStyle = Autodesk.Private.Windows.RibbonListButtonStyle.SplitButton,
                ResizeStyle = RibbonItemResizeStyles.NoResize,
                ListStyle = RibbonSplitButtonListStyle.List
            };
            foreach (ButtonCommand buttonTextData in buttonTextDatas)
            {
                risSplitBtn.Items.Add(CreateButton(buttonTextData));
            }
            risSplitBtn.Current = risSplitBtn.Items.First();
            return risSplitBtn;
        }        
        private class RibbonCommandHandler : System.Windows.Input.ICommand
        {
            public bool CanExecute(object parameter)
            {
                return true;
            }
            public event EventHandler CanExecuteChanged;
            public void Execute(object parameter)
            {
                if (parameter is RibbonButton button)
                {
                    Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
                    Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.
                        SendStringToExecute(button.CommandParameter + " ", true, false, true);
                }
            }      
            
        }
    }

    public class Button
    {
        public Button(string ribbonTabName, string ribbonPanelName)
        {
            RibbonTabName = ribbonTabName;
            RibbonTabId = ribbonTabName + "_Id";
            RibbonPanelName = ribbonPanelName;
        }
        public Button(string ribbonTabName, string ribbonPanelName, List<ButtonCommand> buttonCommnands)
        {
            RibbonTabName = ribbonTabName;
            RibbonTabId = ribbonTabName + "_Id";
            RibbonPanelName = ribbonPanelName;
            ButtonCommands.AddRange(buttonCommnands);
        }
        public Button(string ribbonTabName, string ribbonPanelName, ButtonCommand buttonCommand)
        {
            RibbonTabName = ribbonTabName;
            RibbonTabId = ribbonTabName + "_Id";
            RibbonPanelName = ribbonPanelName;
            ButtonCommands.Add(buttonCommand);
        }
        public string RibbonTabName { get; set; } = string.Empty;
        public string RibbonTabId { get; set; } = string.Empty;
        public string RibbonPanelName { get; set; } = string.Empty;
        public List<ButtonCommand> ButtonCommands { get; set; } = new List<ButtonCommand>();
    }
    public class ButtonCommand
    {
        public ButtonCommand(string command, string name, string content)
        { 
            Command = command;
            Name = name;
            Content = content;
        }        
        public string Command { get; set; } = string.Empty; 
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    } 
}
