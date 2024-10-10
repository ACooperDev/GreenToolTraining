using System;
using System.Collections.Generic;
using System.Linq;
using ViDi2.Training.Local;
//Must be run on an x64 platform.
//Add NuGet packages from: C:\ProgramData\Cognex\VisionPro Deep Learning\3.3\Examples\packages
//ViDi.NET 
//ViDi.NET.VisionPro
namespace GreenTraining
{
    internal class Program
    {
        static void Main(string[] args)
        {

            //Initialize workspace directory
            ViDi2.Training.Local.WorkspaceDirectory workspaceDir = new ViDi2.Training.Local.WorkspaceDirectory();
            //Set the path to workspace directory
            workspaceDir.Path = @"C:\Users\acooper\Desktop\Training";


            //Create a library access instance using the workspace directory
            using(LibraryAccess libraryAccess = new LibraryAccess(workspaceDir))
            {
                //Create a control interface for training tools
                using(ViDi2.Training.IControl myControl = new ViDi2.Training.Local.Control(libraryAccess))
                {

                    //Create a new workspace and add it to the control
                    ViDi2.Training.IWorkspace myWorkspace = myControl.Workspaces.Add("myGreenWorkspace");

                    //Add a new stream to the workspace
                    ViDi2.Training.IStream myStream = myWorkspace.Streams.Add("default");

                    //Add a Red Tool to the stream (for defect detection)
                    ViDi2.Training.IGreenTool myGreenTool = myStream.Tools.Add("Classify", ViDi2.ToolType.Green) as ViDi2.Training.IGreenTool;

                    //Define valid image file extensions
                    List<string> ext = new List<string> { ".jpg", ".bmp", ".png" };

                    //Get all image files in the specified directory that match the extensions
                    IEnumerable<string> imageFiles = System.IO.Directory.GetFiles(
                        @"C:\Users\acooper\Desktop\Training\GreenToolTraining\GreenToolTraining\Images",
                        "*.*",
                        System.IO.SearchOption.TopDirectoryOnly
                    ).Where(s => ext.Any(e => s.EndsWith(e)));

                    //Add each image to the stream's database
                    foreach(string file in imageFiles)
                    {
                        using (ViDi2.FormsImage image = new ViDi2.FormsImage(file))
                        {
                            myStream.Database.AddImage(image, System.IO.Path.GetFileName(file));
                        }
                    }

                    //Process all images in the Green Tool
                    myGreenTool.Database.Process();
                    //Wait until the processing is done
                    myGreenTool.Wait();

                    myGreenTool.Database.Tag("'Good'", "good");
                    myGreenTool.Database.Tag("'Bad'", "bad");
                    myGreenTool.Database.SelectTrainingSet("", 0.5);
                    myGreenTool.Parameters.FeatureSize = new ViDi2.Size(50, 50);
                    myGreenTool.Parameters.Luminance = 0.05;
                    myGreenTool.Parameters.Contrast = 0.05;
                    myGreenTool.Parameters.CountEpochs = 40;

                    //Start training the Green Tool
                    myGreenTool.Train();
                    Console.Write("Start");

                    while(!myGreenTool.Wait(1000))
                    {
                        Console.WriteLine(myGreenTool.Progress.Description + " " + myGreenTool.Progress.ETA.ToString());
                    }

                    //Process the database again after training
                    myGreenTool.Database.Process();
                    myGreenTool.Wait();

                    //Export the runtime workspace to a file
                    using(System.IO.FileStream fs = new System.IO.FileStream(@"C:\Users\acooper\Desktop\Training\GreenToolRuntime.vrws", System.IO.FileMode.Create))
                    {
                        myWorkspace.ExportRuntimeWorkspace().CopyTo(fs);
                    }

                    //Save the workspace
                    myWorkspace.Save();
                }
            }
        }
    }
}

