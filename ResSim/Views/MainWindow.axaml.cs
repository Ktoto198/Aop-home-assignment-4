using Avalonia.Controls;
using ResSim.ViewModels;
using ResSim;
using ResSim.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;
using System.Collections.ObjectModel;

namespace ResSim.Views
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel mainWindowViewModel;
        public string? filePath;

        public MainWindow()
        {
            InitializeComponent();
        }

        // This event will be triggered when the window is loaded (opened)
        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            // Load the JSON data at the start of the application
            filePath = "C:/Users/gojam/OneDrive/Stalinis kompiuteris/Advanced OOP/Homework4/ResSim/exerciseJSON.json";
            LoadFile(filePath);
        }

        // This event will be triggered when the window is loaded
        public async Task LoadFile(string filePath)
        {
            // Instantiate the Parser class
            Console.WriteLine(filePath);
            Parser parser = new Parser(filePath);

            // Initialize recipeData
            MainWindowViewModel.recipeData = parser.GetRecipeData();

            mainWindowViewModel = new MainWindowViewModel(this);
            DataContext = mainWindowViewModel;

            if (MainWindowViewModel.recipeData?.recipes != null)
            {
                await mainWindowViewModel.StartMealProcessing(MainWindowViewModel.recipeData.recipes);
            }
        }

    }
}