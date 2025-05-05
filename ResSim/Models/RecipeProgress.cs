using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ResSim.Models
{
    public partial class RecipeProgress : ObservableObject
    {
        // Observable properties automatically generate the backing fields
        [ObservableProperty]
        public string recipeName;

        [ObservableProperty]
        public double progress;

        [ObservableProperty]
        public string status;

        [ObservableProperty]
        public string currentStep;

        public RecipeProgress()
        {
            RecipeName = string.Empty; // Initialize with an empty string
            CurrentStep = string.Empty; // Initialize with an empty string
            Status = "Not Started"; // Initialize status as Not Started
            Progress = 0.0; // Initialize progress to 0%
        }

        // Method to update the progress based on the completed steps
        public double UpdateProgress(int completedSteps, int totalSteps)
        {
            if (totalSteps == 0) return 0; // Avoid division by zero

            Progress = (double)completedSteps / totalSteps * 100;
            
            return Progress;
        }

        // Method to mark the progress as completed
        public void MarkAsCompleted()
        {
            Status = "Completed";
            Progress = 100; // Set progress to 100% when completed
        }
    }
}