using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FileManager3;

public class DragDropHandler
{
    public static void EnableDragDrop(UIElement sourceElement, UIElement targetElement)
    {
        sourceElement.PreviewMouseLeftButtonDown += SourceElement_PreviewMouseLeftButtonDown;
        targetElement.AllowDrop = true;
        targetElement.Drop += TargetElement_Drop;
    }

    private static void SourceElement_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is string filePath)
        {
            DragDrop.DoDragDrop(element, filePath, DragDropEffects.Copy);
        }
    }

    private static void TargetElement_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string droppedFilePath = e.Data.GetData(DataFormats.FileDrop) as string;
            MessageBox.Show($"File or folder dropped: {droppedFilePath}");
            // Add logic to handle the dropped file or folder
        }
    }
}
