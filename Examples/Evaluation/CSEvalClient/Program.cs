﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//
// Program.cs -- main C# file that contains client code to call the CLI Wrapper class.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.MSR.CNTK.Extensibility.Managed;

namespace Microsoft.MSR.CNTK.Extensibility.Managed.CSEvalClient
{
    /// <summary>
    /// Program for demonstrating how to run model evaluations using the CLIWrapper
    /// </summary>
    /// <description>
    /// This program is a managed client using the CLIWrapper to run the model evaluator in CNTK.
    /// There are four cases shown in this program related to model loading, network creation and evaluation.
    /// 
    /// To run this program from the CNTK binary drop, you must add the NuGet package for model evaluation first.
    /// Refer to <see cref="https://github.com/Microsoft/CNTK/wiki/NuGet-Package"/> for information regarding the NuGet package for model evaluation.
    /// 
    /// EvaluateModelSingleLayer and EvaluateModelMultipleLayers
    /// --------------------------------------------------------
    /// These two cases require the 01_OneHidden model which is part of the <CNTK>/Examples/Image/MNIST example.
    /// Refer to <see cref="https://github.com/Microsoft/CNTK/blob/master/Examples/Image/MNIST/README.md"/> for how to train
    /// the model used in these examples.
    /// 
    /// EvaluateNetworkSingleLayer and EvaluateNetworkSingleLayerNoInput
    /// ----------------------------------------------------------------
    /// These two cases do not required a trained model (just the network description). These cases show how to extract values from a single forward-pass
    /// without any input to the model.
    /// 
    /// EvaluateMultipleModels
    /// ----------------------
    /// This case requires the 02_Convolution model and the Test-28x28.txt test file which are part of the <CNTK>/Examples/Image/MNIST example.
    /// Refer to <see cref="https://github.com/Microsoft/CNTK/blob/master/Examples/Image/MNIST/README.md"/> for how to train
    /// the model used in this example.
    /// 
    /// EvaluateModelImageInput
    /// -----------------------
    /// This case requires the ResNet_34 trained model which is part of the <CNTK>/Examples/Image/Miscellanous/ImageNet/ResNet</CNTK> example.
    /// This case shows how to evaluate a model that was trained with the ImageReader.
    /// The input for evaluation needs to be transformed in a similar manner as the ImageReader did for training.
    /// 
    /// </description>
    class Program
    {
        private static string initialDirectory;

        /// <summary>
        /// Program entry point
        /// </summary>
        /// <param name="args">Program arguments (ignored)</param>
        private static void Main(string[] args)
        {
            initialDirectory = Environment.CurrentDirectory;
            /*
            Console.WriteLine("====== EvaluateModelSingleLayer ========");
            EvaluateModelSingleLayer();

            Console.WriteLine("\n====== EvaluateModelMultipleLayers ========");
            EvaluateModelMultipleLayers();

            Console.WriteLine("\n====== EvaluateNetworkSingleLayer ========");
            EvaluateNetworkSingleLayer();

            Console.WriteLine("\n====== EvaluateNetworkSingleLayerNoInput ========");
            EvaluateNetworkSingleLayerNoInput();

            Console.WriteLine("\n====== EvaluateExtendedNetworkSingleLayerNoInput ========");
            EvaluateExtendedNetworkSingleLayerNoInput();

            Console.WriteLine("\n====== EvaluateMultipleModels ========");
            EvaluateMultipleModels();

            Console.WriteLine("\n====== EvaluateModelWithImageInput ========");
            EvaluateModelImageInput();
            */
            Console.WriteLine("\n====== EvaluateModelImageInput ========");
            EvaluateImageClassificationModel();

            Console.WriteLine("Press <Enter> to terminate.");
            Console.ReadLine();
        }

        /// <summary>
        /// Evaluates a trained model and obtains a single layer output
        /// </summary>
        /// <remarks>
        /// This example requires the 01_OneHidden trained model
        /// </remarks>
        private static void EvaluateModelSingleLayer()
        {
            try
            {
                string outputLayerName;

                // The examples assume the executable is running from the data folder
                // We switch the current directory to the data folder (assuming the executable is in the <CNTK>/x64/Debug|Release folder
                Environment.CurrentDirectory = Path.Combine(initialDirectory, @"..\..\Examples\Image\MNIST\Data\");
                List<float> outputs;

                using (var model = new IEvaluateModelManagedF())
                {
                    // Load model
                    string modelFilePath = Path.Combine(Environment.CurrentDirectory, @"..\Output\Models\01_OneHidden");
                    model.CreateNetwork(string.Format("modelPath=\"{0}\"", modelFilePath), deviceId: -1);

                    // Generate random input values in the appropriate structure and size
                    var inDims = model.GetNodeDimensions(NodeGroup.Input);
                    var inputs = GetDictionary(inDims.First().Key, inDims.First().Value, 255);

                    // We request the output layer names(s) and dimension, we'll use the first one.
                    var outDims = model.GetNodeDimensions(NodeGroup.Output);
                    outputLayerName = outDims.First().Key;
                    // We can call the evaluate method and get back the results (single layer)...
                    outputs = model.Evaluate(inputs, outputLayerName);
                }

                OutputResults(outputLayerName, outputs);
            }
            catch (CNTKException ex)
            {
                Console.WriteLine("Error: {0}\nNative CallStack: {1}\n Inner Exception: {2}", ex.Message, ex.NativeCallStack, ex.InnerException != null ? ex.InnerException.Message : "No Inner Exception");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}\nCallStack: {1}\n Inner Exception: {2}", ex.Message, ex.StackTrace, ex.InnerException != null ? ex.InnerException.Message : "No Inner Exception");
            }
        }

        /// <summary>
        /// Evaluates a trained model and obtains multiple layers output (including hidden layer)
        /// </summary>
        /// <remarks>
        /// This example requires the 01_OneHidden trained model
        /// </remarks>
        private static void EvaluateModelMultipleLayers()
        {
            try
            {
                // The examples assume the executable is running from the data folder
                // We switch the current directory to the data folder (assuming the executable is in the <CNTK>/x64/Debug|Release folder
                Environment.CurrentDirectory = Path.Combine(initialDirectory, @"..\..\Examples\Image\MNIST\Data\");

                Dictionary<string, List<float>> outputs;

                using (var model = new IEvaluateModelManagedF())
                {
                    // Desired output layers
                    const string hiddenLayerName = "h1.z";
                    const string outputLayerName = "ol.z";

                    // Load model
                    string modelFilePath = Path.Combine(Environment.CurrentDirectory, @"..\Output\Models\01_OneHidden");
                    var desiredOutputLayers = new List<string>() { hiddenLayerName, outputLayerName };
                    model.CreateNetwork(string.Format("modelPath=\"{0}\"", modelFilePath), deviceId: -1, outputNodeNames: desiredOutputLayers);

                    // Generate random input values in the appropriate structure and size
                    var inDims = model.GetNodeDimensions(NodeGroup.Input);
                    var inputs = GetDictionary(inDims.First().Key, inDims.First().Value, 255);

                    // We request the output layer names(s) and dimension, we'll get both the hidden layer and the output layer
                    var outDims = model.GetNodeDimensions(NodeGroup.Output);

                    // We can preallocate the output structure and pass it in (multiple output layers)
                    outputs = new Dictionary<string, List<float>>()
                    {
                        { hiddenLayerName, GetFloatArray(outDims[hiddenLayerName], 1) },    
                        { outputLayerName, GetFloatArray(outDims[outputLayerName], 1) }
                    };
                    model.Evaluate(inputs, outputs);
                }

                OutputResults(outputs);
            }
            catch (CNTKException ex)
            {
                Console.WriteLine("Error: {0}\nNative CallStack: {1}\n Inner Exception: {2}", ex.Message, ex.NativeCallStack, ex.InnerException != null ? ex.InnerException.Message : "No Inner Exception");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}\nCallStack: {1}\n Inner Exception: {2}", ex.Message, ex.StackTrace, ex.InnerException != null ? ex.InnerException.Message : "No Inner Exception");
            }
        }

        /// <summary>
        /// Evaluates a network (without a model, but requiring input) and obtains a single layer output
        /// </summary>
        private static void EvaluateNetworkSingleLayer()
        {
            try
            {
                // The examples assume the executable is running from the data folder
                // We switch the current directory to the data folder (assuming the executable is in the <CNTK>/x64/Debug|Release folder
                string workingDirectory = Path.Combine(initialDirectory, @"..\..\Examples\Other\Simple2d\Config");
                Environment.CurrentDirectory = initialDirectory;

                List<float> outputs;
                string outputLayerName;

                using (var model = new IEvaluateModelManagedF())
                {
                    // Create the network
                    // This network (AddOperatorConstant.cntk) is a simple network consisting of a single binary operator (Plus)
                    // operating over a single input and a constant
                    string networkDescription = File.ReadAllText(Path.Combine(workingDirectory, @"AddOperatorConstant.cntk"));
                    model.CreateNetwork(networkDescription, deviceId: -1);

                    // Prepare input value in the appropriate structure and size
                    var inputs = new Dictionary<string, List<float>>() { { "features", new List<float>() { 1.0f } } };

                    // We can call the evaluate method and get back the results (single layer output)...
                    var outDims = model.GetNodeDimensions(NodeGroup.Output);
                    outputLayerName = outDims.First().Key;
                    outputs = model.Evaluate(inputs, outputLayerName);
                }

                OutputResults(outputLayerName, outputs);
            }
            catch (CNTKException ex)
            {
                Console.WriteLine("Error: {0}\nNative CallStack: {1}\n Inner Exception: {2}", ex.Message, ex.NativeCallStack, ex.InnerException != null ? ex.InnerException.Message : "No Inner Exception");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}\nCallStack: {1}\n Inner Exception: {2}", ex.Message, ex.StackTrace, ex.InnerException != null ? ex.InnerException.Message : "No Inner Exception");
            }
        }

        /// <summary>
        /// Evaluates a network (without a model and without input) and obtains a single layer output
        /// </summary>
        private static void EvaluateNetworkSingleLayerNoInput()
        {
            try
            {
                // The examples assume the executable is running from the data folder
                // We switch the current directory to the data folder (assuming the executable is in the <CNTK>/x64/Debug|Release folder
                string workingDirectory = Path.Combine(initialDirectory, @"..\..\Examples\Other\Simple2d\Config");
                Environment.CurrentDirectory = initialDirectory;

                List<float> outputs;

                using (var model = new IEvaluateModelManagedF())
                {
                    // Create the network
                    // This network (AddOperatorConstantNoInput.cntk) is a simple network consisting of a single binary operator (Plus)
                    // operating over a two constants, therefore no input is necessary.
                    string networkDescription = File.ReadAllText(Path.Combine(workingDirectory, @"AddOperatorConstantNoInput.cntk"));
                    model.CreateNetwork(networkDescription, deviceId: -1);

                    // We can call the evaluate method and get back the results (single layer)...
                    outputs = model.Evaluate("ol", 1);
                }

                OutputResults("ol", outputs);
            }
            catch (CNTKException ex)
            {
                Console.WriteLine("Error: {0}\nNative CallStack: {1}\n Inner Exception: {2}", ex.Message, ex.NativeCallStack, ex.InnerException != null ? ex.InnerException.Message : "No Inner Exception");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}\nCallStack: {1}\n Inner Exception: {2}", ex.Message, ex.StackTrace, ex.InnerException != null ? ex.InnerException.Message : "No Inner Exception");
            }
        }

        /// <summary>
        /// Evaluates an extended network (without a model and without input) and obtains a single layer output
        /// </summary>
        private static void EvaluateExtendedNetworkSingleLayerNoInput()
        {
            const string modelDefinition = @"precision = ""float"" 
                                     traceLevel = 1
                                     run=NDLNetworkBuilder
                                     NDLNetworkBuilder=[
                                     v1 = Constant(1)
                                     v2 = Constant(2, tag=""output"")
                                     ol = Plus(v1, v2, tag=""output"")
                                     FeatureNodes = (v1)
                                     ]";

            try
            {
                // The examples assume the executable is running from the data folder
                // We switch the current directory to the data folder (assuming the executable is in the <CNTK>/x64/Debug|Release folder
                string workingDirectory = Path.Combine(initialDirectory, @"..\..\Examples\Other\Simple2d\Config");
                Environment.CurrentDirectory = initialDirectory;

                using (var model = new ModelEvaluationExtendedF())
                {
                    // Create the network
                    // This network (AddOperatorConstantNoInput.cntk) is a simple network consisting of a single binary operator (Plus)
                    // operating over a two constants, therefore no input is necessary.
                    model.CreateNetwork(modelDefinition);

                    VariableSchema outputSchema = model.GetOutputSchema();

                    var outputNodeNames = outputSchema.Select(s => s.Name).ToList<string>();
                    model.StartForwardEvaluation(outputNodeNames);

                    var outputBuffer = outputSchema.CreateBuffers<float>();
                    var inputBuffer = new ValueBuffer<float>[0];

                    // We can call the evaluate method and get back the results...
                    model.ForwardPass(inputBuffer, outputBuffer);

                    // We expect two outputs: the v2 constant, and the ol Plus result
                    var expected = new float[][] { new float[] { 2 }, new float[] { 3 } };

                    Console.WriteLine("Expected values: {0}", string.Join(" - ", expected.Select(b => string.Join(", ", b)).ToList<string>()));
                    Console.WriteLine("Actual Values  : {0}", string.Join(" - ", outputBuffer.Select(b => string.Join(", ", b.Buffer)).ToList<string>()));
                }
            }
            catch (CNTKException ex)
            {
                Console.WriteLine("Error: {0}\nNative CallStack: {1}\n Inner Exception: {2}", ex.Message, ex.NativeCallStack, ex.InnerException != null ? ex.InnerException.Message : "No Inner Exception");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}\nCallStack: {1}\n Inner Exception: {2}", ex.Message, ex.StackTrace, ex.InnerException != null ? ex.InnerException.Message : "No Inner Exception");
            }
        }

        /// <summary>
        /// Evaluates multiple instances of a model in the same process.
        /// </summary>
        /// <remarks>
        /// Although all models execute concurrently (multiple tasks), each model is evaluated with a single task at a time.
        /// </remarks>
        private static void EvaluateMultipleModels()
        {
            // Specifies the number of models in memory as well as the number of parallel tasks feeding these models (1 to 1)
            int numConcurrentModels = 4;

            // Specifies the number of times to iterate through the test file (epochs)
            int numRounds = 1;

            // Counts the number of evaluations accross all models
            int count = 0;

            // Counts the number of failed evaluations (output != expected) accross all models
            int errorCount = 0;

            // The examples assume the executable is running from the data folder
            // We switch the current directory to the data folder (assuming the executable is in the <CNTK>/x64/Debug|Release folder
            Environment.CurrentDirectory = Path.Combine(initialDirectory, @"..\..\Examples\Image\MNIST\Data\");

            // Load model
            string modelFilePath = Path.Combine(Environment.CurrentDirectory, @"..\Output\Models\02_Convolution");

            // Initializes the model instances
            ModelEvaluator.Initialize(numConcurrentModels, modelFilePath);

            string testfile = Path.Combine(Environment.CurrentDirectory, @"Test-28x28.txt");
            Stopwatch sw = new Stopwatch();
            sw.Start();

            try
            {
                for (int i = 0; i < numRounds; i++)
                {
                    // Feed each line to a single model in parallel
                    Parallel.ForEach(File.ReadLines(testfile), new ParallelOptions() { MaxDegreeOfParallelism = numConcurrentModels }, (line) =>
                    {
                        Interlocked.Increment(ref count);

                        // The first value in the line is the expected label index for the record's outcome
                        int expected = int.Parse(line.Substring(0, line.IndexOf('\t')));
                        var inputs = line.Substring(line.IndexOf('\t') + 1).Split('\t').Select(float.Parse).ToList();

                        // We can call the evaluate method and get back the results (single layer)...
                        var outputs = ModelEvaluator.Evaluate(inputs);

                        // Retrieve the outcome index (so we can compare it with the expected index)
                        int index = 0;
                        var max = outputs.Select(v => new { Value = v, Index = index++ })
                            .Aggregate((a, b) => (a.Value > b.Value) ? a : b)
                            .Index;

                        // Count the errors
                        if (expected != max)
                        {
                            Interlocked.Increment(ref errorCount);
                        }
                    });
                }
            }
            catch (CNTKException ex)
            {
                Console.WriteLine("Error: {0}\nNative CallStack: {1}\n Inner Exception: {2}", ex.Message, ex.NativeCallStack, ex.InnerException != null ? ex.InnerException.Message : "No Inner Exception");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}\nCallStack: {1}\n Inner Exception: {2}", ex.Message, ex.StackTrace, ex.InnerException != null ? ex.InnerException.Message : "No Inner Exception");
            }

            sw.Stop();
            ModelEvaluator.DisposeAll();
            
            Console.WriteLine("The file {0} was processed using {1} concurrent model(s) with an error rate of: {2:P2} ({3} error(s) out of {4} record(s)), and a throughput of {5:N2} records/sec", @"Test-28x28.txt", 
                numConcurrentModels, (float)errorCount / count, errorCount, count, (count + errorCount) * 1000.0 / sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// Evaluates a trained model with an image as input and obtains a single layer output
        /// </summary>
        /// <remarks>
        /// This example requires the ResNet_34 trained model
        /// </remarks>
        private static void EvaluateModelImageInput()
        {
            try
            {
                var testBitmap = new Bitmap(Bitmap.FromFile(@"E:\TestPattern2.bmp")).Resize(15, 15, true);
                int iter = 1;

                Stopwatch sw = new Stopwatch();
                List<float> list1 = new List<float>();
                List<float> list2 = new List<float>();
                List<float> list3 = new List<float>();
                List<float> list4 = new List<float>();
                
                sw.Start();
                for (int i = 0; i < iter; i++)
                {
                    list1 = testBitmap.ExtractHWC();
                }
                sw.Stop();
                Console.WriteLine("ExtractHWC = {0} ms/call", sw.ElapsedMilliseconds / iter);
                
                sw.Reset();
                sw.Start();
                for (int i = 0; i < iter; i++)
                {
                    list2 = testBitmap.ParallelExtractHWC();
                }
                sw.Stop();
                Console.WriteLine("ParallelExtractHWC = {0} ms/call", sw.ElapsedMilliseconds / iter);
                
                sw.Reset();
                sw.Start();
                for (int i = 0; i < iter; i++)
                {
                    list3 = testBitmap.ExtractCHW();
                }
                sw.Stop();
                Console.WriteLine("ExtractCHW = {0} ms/call", sw.ElapsedMilliseconds / iter);
                
                sw.Reset();
                sw.Start();
                for (int i = 0; i < iter; i++)
                {
                    list4 = testBitmap.ParallelExtractCHW();
                }
                sw.Stop();
                Console.WriteLine("ParallelExtractCHW = {0} ms/call", sw.ElapsedMilliseconds / iter);

                OutputListComparison("\nComparing HWC to Parallel HWC", list1, list2);

                OutputListComparison("\nComparing CHW to Parallel CHW", list3, list4);
            }
            catch (CNTKException ex)
            {
                Console.WriteLine("Error: {0}\nNative CallStack: {1}\n Inner Exception: {2}", ex.Message, ex.NativeCallStack, ex.InnerException != null ? ex.InnerException.Message : "No Inner Exception");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}\nCallStack: {1}\n Inner Exception: {2}", ex.Message, ex.StackTrace, ex.InnerException != null ? ex.InnerException.Message : "No Inner Exception");
            }
        }

        /// <summary>
        /// This method shows how to evaluate a trained image classification model
        /// </summary>
        public static void EvaluateImageClassificationModel()
        {
            try
            {
                // The examples assume the executable is running from the data folder
                // We switch the current directory to the data folder (assuming the executable is in the <CNTK>/x64/Debug|Release folder
                string workingDirectory = Path.Combine(initialDirectory, @"..\..\Examples\Image\Miscellaneous\ImageNet\ResNet");
                Environment.CurrentDirectory = initialDirectory;

                List<float> outputs;
                string outputLayerName;

                using (var model = new IEvaluateModelManagedF())
                {
                    string modelFilePath = @"\\GAIZKA-DESKTOP\CNTK\Models\ResNet_18.model";
                    model.CreateNetwork(string.Format("modelPath=\"{0}\"", modelFilePath), deviceId: -1);

                    // Prepare input value in the appropriate structure and size
                    var inDims = model.GetNodeDimensions(NodeGroup.Input);
                    //var inputs = GetDictionary(inDims.First().Key, inDims.First().Value, 1);

                    // Transform the image
                    string imageFileName = @"E:\Zebra04.jpg";
                    Bitmap bmp = new Bitmap(Bitmap.FromFile(imageFileName));
                    var resized = bmp.Resize(224, 224, true);
                    var resizedCHW = bmp.ParallelExtractCHW();
                    var inputs = new Dictionary<string, List<float>>() { {inDims.First().Key, resizedCHW } };

                    // We can call the evaluate method and get back the results (single layer output)...
                    var outDims = model.GetNodeDimensions(NodeGroup.Output);
                    outputLayerName = outDims.First().Key;
                    outputs = model.Evaluate(inputs, outputLayerName);
                }

                // Retrieve the outcome index (so we can compare it with the expected index)
                int index = 0;
                var max = outputs.Select(v => new { Value = v, Index = index++ })
                    .Aggregate((a, b) => (a.Value > b.Value) ? a : b)
                    .Index;

                //OutputResults(outputLayerName, outputs);
                Console.WriteLine("Outcome: {0}", max);
            }
            catch (CNTKException ex)
            {
                Console.WriteLine("Error: {0}\nNative CallStack: {1}\n Inner Exception: {2}", ex.Message, ex.NativeCallStack, ex.InnerException != null ? ex.InnerException.Message : "No Inner Exception");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}\nCallStack: {1}\n Inner Exception: {2}", ex.Message, ex.StackTrace, ex.InnerException != null ? ex.InnerException.Message : "No Inner Exception");
            }
        }

        /// <summary>
        /// Dumps the output to the console
        /// </summary>
        /// <param name="outputs">The structure containing the output layers</param>
        private static void OutputResults(Dictionary<string, List<float>> outputs)
        {
            Console.WriteLine("--- Output results ---");
            foreach (var item in outputs)
            {
                OutputResults(item.Key, item.Value);
            }
        }

        /// <summary>
        /// Dumps the output of a layer to the console
        /// </summary>
        /// <param name="layer">The display name for the layer</param>
        /// <param name="values">The layer values</param>
        private static void OutputResults(string layer, List<float> values)
        {
            if (values == null)
            {
                Console.WriteLine("No Output for layer: {0}", layer);
                return;
            }

            Console.WriteLine("Output layer: {0}", layer);
            foreach (var entry in values)
            {
                Console.WriteLine(entry);
            }
        }

        /// <summary>
        /// Creates a Dictionary for input entries or output allocation 
        /// </summary>
        /// <param name="key">The key for the mapping</param>
        /// <param name="size">The number of element entries associated to the key</param>
        /// <param name="maxValue">The maximum value for random generation values</param>
        /// <returns>A dictionary with a single entry for the key/values</returns>
        static Dictionary<string, List<float>> GetDictionary(string key, int size, int maxValue)
        {
            var dict = new Dictionary<string, List<float>>();
            if (key != string.Empty && size >= 0 && maxValue > 0)
            {
                dict.Add(key, GetFloatArray(size, maxValue));
            }

            return dict;
        }

        /// <summary>
        /// Creats a list of random numbers
        /// </summary>
        /// <param name="size">The size of the list</param>
        /// <param name="maxValue">The maximum value for the generated values</param>
        /// <returns>A list of random numbers</returns>
        static List<float> GetFloatArray(int size, int maxValue)
        {
            List<float> list = new List<float>();
            if (size > 0 && maxValue >= 0)
            {
                Random rnd = new Random();
                list.AddRange(Enumerable.Range(1, size).Select(i => (float)rnd.Next(maxValue)).ToList());
            }

            return list;
        }

        static void OutputList(string caption, List<float> list)
        {
            Console.WriteLine(caption);

            for (int i = 0; i < list.Count; i++)
            {
                if (i % 15 == 0 && i > 0)
                    Console.WriteLine();

                Console.Write("{0,3}  ", list[i]);
            }
            Console.WriteLine("\n");
        }

        static void OutputListComparison(string caption, List<float> listA, List<float> listB)
        {
            bool different = false;
            Console.WriteLine(caption);

            if (listA.Count != listB.Count)
                Console.WriteLine("List sizes don't match: A={0}, B={1}", listA.Count, listB.Count);

            for (int i = 0; i < listA.Count; i++)
            {
                if (i % 10 == 0 && different)
                    Console.WriteLine();

                if ((int) listA[i] != (int) listB[i])
                {
                    different = true;
                    Console.Write("{0,3}/{1,3}  ", listA[i], listB[i]);
                }
            }

            if (!different)
                Console.WriteLine("Lists are equal.");
        }
    }
}
