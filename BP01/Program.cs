using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace BP01
{
    class Program
    {
        public static Logger logger;

        static void Main(string[] args)
        {
            try
            {
                // Declaration of variables.

                Settings settings = new Settings();
                settings.MaxExcludable = 0;
                settings.PrintSteps = false;
                settings.OutputFileName = null;
                settings.Output = Console.Out;
                settings.LogMode = 2; // 0 = no logging, 1 = just result, 2 = all
                settings.Mode = 3;
                    // 1 = Select - Exclude short fragments - Add loops - Add ends - Pad to N
                    // 2 = Select - Add loops - Exclude short fragments - Add ends - Pad to N
                    // 3 = Select - Add loops - Add ends - Exclude short fragments - Pad to N
                settings.RunScript = false;
                settings.ScriptInCommandLineMode = false;
                settings.QuitAfterScript = false;
                settings.Any = false;

                State state = new State();


                // Processing the options.
                                
                List<String> argList = new List<string>(args);

                try
                {
                    while (argList[0].StartsWith("-"))
                    {
                        switch (argList[0])
                        {
                            case "-a":
                                settings.Any = true;
                                argList.RemoveAt(0);
                                break;
                            case "-e":
                                int me;
                                if (Int32.TryParse(argList[1], out me))
                                {
                                    settings.MaxExcludable = me;
                                }
                                else
                                {
                                    Console.Error.WriteLine("Wrong format of argument of \"-e\": integer expected, \"" + argList[1] + "\" found.");
                                    return;
                                }
                                argList.RemoveAt(0);
                                argList.RemoveAt(0);
                                break;
                            case "-l":
                                int lm;
                                if (Int32.TryParse(argList[1], out lm) && Settings.LOG_MODES.Contains(lm))
                                {
                                    settings.LogMode = lm;
                                }
                                else
                                {
                                    Console.Error.WriteLine("Wrong format of argument of \"-l\": integer from 0 to 2 expected, \"" + argList[1] + "\" found.");
                                    return;
                                }
                                argList.RemoveAt(0);
                                argList.RemoveAt(0);
                                break;
                            case "-m":
                                int m;
                                if (Int32.TryParse(argList[1], out m) && Settings.MODES.Contains(m))
                                {
                                    settings.Mode = m;
                                }
                                else
                                {
                                    Console.Error.WriteLine("Wrong format of argument of \"-m\": integer from 1 to 5 expected, \"" + argList[1] + "\" found.");
                                    return;
                                }
                                argList.RemoveAt(0);
                                argList.RemoveAt(0);
                                break;
                            case "-o":
                                settings.OutputFileName = argList[1];
                                argList.RemoveAt(0);
                                argList.RemoveAt(0);
                                break;
                            case "-r":
                                settings.RunScript = true;
                                settings.QuitAfterScript = true;
                                argList.RemoveAt(0);
                                break;
                            case "-rc":
                                settings.RunScript = true;
                                settings.ScriptInCommandLineMode = true;
                                settings.QuitAfterScript = true;
                                argList.RemoveAt(0);
                                break;
                            case "-ro":
                                settings.RunScript = true;
                                argList.RemoveAt(0);
                                break;
                            case "-s":
                                settings.PrintSteps = true;
                                argList.RemoveAt(0);
                                break;
                            default:
                                Console.Error.WriteLine("Unknown option: \"" + argList[0] + "\"");
                                return;
                        }
                    }


                    // Processing the arguments.

                    settings.InputFileName = argList[0];
                    
                    int cai;
                    if (Int32.TryParse(argList[1], out cai))
                    {
                        settings.CentralAtomIndex = cai;
                    }
                    else
                    {
                        Console.Error.WriteLine("Wrong format of 2nd argument: integer expected.");
                        return;
                    }

                    double r;
                    if (Double.TryParse(argList[2], NumberStyles.Float, new CultureInfo("en-US"), out r))
                    {
                        settings.Radius = r;
                    }
                    else
                    {
                        Console.Error.WriteLine("Wrong format of 3rd argument: real number expected.");
                        return;
                    }

                    int n;
                    if (Int32.TryParse(argList[3], out n))
                    {
                        settings.N = n;
                    }
                    else
                    {
                        Console.Error.WriteLine("Wrong format of 4th argument: integer expected.");
                        return;
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    Console.Error.WriteLine("Wrong number or type of arguments: 4 expected, " + argList.Count + " found.");
                    Console.Error.WriteLine("   argument 1: input PDB file");
                    Console.Error.WriteLine("   argument 2: index of the central atom");
                    Console.Error.WriteLine("   argument 3: radius in Angstroms");
                    Console.Error.WriteLine("   argument 4: maximum number of atoms");
                    Console.Error.WriteLine("Options:");
                    Console.Error.WriteLine("   -a                   Selects residues in radius by any atom");
                    Console.Error.WriteLine("                        (default = by C alpha atom).");
                    Console.Error.WriteLine("   -e maxExcludable     Maximum length of chain fragment, that can be excluded");
                    Console.Error.WriteLine("                        from the selection (default = 0).");
                    Console.Error.WriteLine("   -l loggingMode       Changes the mode of logging.");
                    Console.Error.WriteLine("                        0 = no logging, 1 = just result, 2 (default) = all");
                    Console.Error.WriteLine("   -m mode              Changes the mode (order of steps).");
                    Console.Error.WriteLine("                        1 = Select - Exclude short fragments - Add loops -");
                    Console.Error.WriteLine("                            - Add ends - Pad to N atoms");
                    Console.Error.WriteLine("                        2 = Select - Add loops - Exclude short fragments -");
                    Console.Error.WriteLine("                            - Add ends - Pad to N atoms");
                    Console.Error.WriteLine("                        3 (default) = Select - Add loops - Add ends -");
                    Console.Error.WriteLine("                            - Exclude short fragments - Pad to N atoms");
                    Console.Error.WriteLine("                        4 = Select - Add loops - Exclude short fragments -");
                    Console.Error.WriteLine("                            - Add loops - Add ends - Pad to N atoms");
                    Console.Error.WriteLine("                        5 = Select - Add loops - Add ends -");
                    Console.Error.WriteLine("                            - Exclude short fragments - Add loops -");
                    Console.Error.WriteLine("                            - Add ends - Pad to N atoms");
                    Console.Error.WriteLine("   -o outputFileName    Sets the file for output (default = standard output).");
                    Console.Error.WriteLine("   -r                   Run PyMOL script to make a .pse file.");
                    Console.Error.WriteLine("   -rc                  Run PyMOL script to make a .pse file,");
                    Console.Error.WriteLine("                        using command line version of PyMOL.");
                    Console.Error.WriteLine("   -ro                  Run PyMOL script to make a .pse file, keep PyMOL open.");
                    Console.Error.WriteLine("   -s                   Prints result of each step in a separate file.");
                    return;
                }


                // Opening the input file.
                try
                    {
                        settings.Input = new StreamReader(settings.InputFileName);
                    }
                    catch (ArgumentException)
                    {
                        Console.Error.WriteLine("File name is empty string.");
                        return;
                    }
                    catch (FileNotFoundException)
                    {
                        Console.Error.WriteLine("File \"" + settings.InputFileName + "\" not found.");
                        return;
                    }
                    catch (DirectoryNotFoundException)
                    {
                        Console.Error.WriteLine("Directory not found.");
                        return;
                    }
                    catch (IOException)
                    {
                        Console.Error.WriteLine("IOException while accessing file " + settings.InputFileName + ".");
                        return;
                    }

                // Creating the output file.
                try
                {
                    settings.Output = new StreamWriter(settings.OutputFileName);
                }
                catch (IOException ex)
                {
                    Console.Error.WriteLine("Cannot write to file \"" + settings.OutputFileName + "\".");
                    return;
                }
                                

                // Creating the log file.

                if (settings.LogMode >= 1)
                {
                    if (settings.OutputFileName != null)
                    {
                        String logFileName = Path.GetDirectoryName(settings.OutputFileName)
                            + Path.DirectorySeparatorChar
                            + Path.GetFileNameWithoutExtension(settings.OutputFileName) + "_log.xml";
                        Program.logger = new Logger(new StreamWriter(logFileName), Logger.Level.INFO);
                    }
                    else
                    {
                        Program.logger = new Logger(Console.Error);
                    }
                    if (settings.LogMode == 1) Program.logger.TotalIgnore = true;
                }
                else
                {
                    Program.logger = new Logger(Console.Error, true);
                }

                
                // Logging the settings.

                if (settings.LogMode == 1) Program.logger.TotalIgnore = false;
                Program.logger.OpenBlock("Settings");
                Program.logger.Info("Input file = \"" + settings.InputFileName + "\"");
                Program.logger.Info("Index of the central atom = " + settings.CentralAtomIndex);
                Program.logger.Info("Radius = " + settings.Radius);
                Program.logger.Info("Maximal number of atoms = " + settings.N);
                Program.logger.Info("Maximal length of excludable fragment = " + settings.MaxExcludable);
                if (settings.OutputFileName == null) Program.logger.Info("Output file = standard output");
                else Program.logger.Info("Output file = \"" + settings.OutputFileName + "\"");
                Program.logger.Info("Mode = " + settings.Mode);
                Program.logger.CloseBlock("Settings");
                if (settings.LogMode == 1) Program.logger.TotalIgnore = true;
                

                // Parsing the input file.

                Program.logger.OpenBlock("Parsing the input file.");
                settings.Protein = new Protein(settings.Input);
                Program.logger.Info(settings.Protein.GetChains().Count + " chains found.");
                Program.logger.Info(settings.Protein.GetResidues().Count + " residues found.");
                Program.logger.Info(settings.Protein.GetAtoms().Count + " atoms found.");
                Program.logger.CloseBlock("Parsing the input file.");


                // Finding the central atom.

                try
                {
                    settings.CentralAtom = settings.Protein.GetAtoms().First<Atom>(delegate(Atom atom) { return atom.Serial == settings.CentralAtomIndex; });
                    Program.logger.Info("Central atom found: " + settings.CentralAtom);
                }
                catch (InvalidOperationException)
                {
                    Console.Error.WriteLine("Atom with index " + settings.CentralAtomIndex + " not found.");
                    Program.logger.Fatal("Atom with index " + settings.CentralAtomIndex + " not found.");
                    Program.logger.CloseFatal();
                    return;
                }

                // Removing residues with no C alpha atom.

                settings.Protein = settings.Protein.KeepOnlyNormalResidues();


                // Selecting atoms for output.

                SortedSet<Atom> selectedAtoms = RunAlgorithm(state, settings);
                

                // Output.

                foreach (Atom atom in selectedAtoms)
                {
                    settings.Output.WriteLine(atom);
                }
                settings.Output.WriteLine();


                // Running a script for PyMOL.

                if (settings.RunScript)
                {
                    RunPyMOLScript(settings);
                }


                // Writting summary on standard output.

                SortedSet<Chain> chainFragments = state.Builder.GetChains();
                Console.WriteLine("Number of chain fragments: " + chainFragments.Count);
                Console.WriteLine("Number of loops: " + state.Loops.Count);
                Console.WriteLine("Number of ends: " + state.Ends.Count);
                Console.WriteLine("Number of isolated fragments: " + state.Isolated.Count);
                Console.WriteLine("Number of selected residues: " + Lib.GetResiduesOfAll(chainFragments).Count);
                int numAtoms=Lib.GetAtomsOfAll(chainFragments).Count;
                Console.WriteLine("Number of selected atoms: " + numAtoms);
                Console.WriteLine("Number of backbone cuttings: " + (2*state.Loops.Count + state.Ends.Count));
                if (numAtoms > settings.N)
                {
                    Console.WriteLine("Warning: the number of selected atoms (" + numAtoms 
                        + ") is larger than the maximum number specified by the parameter 4 (" + settings.N + ")!");
                    Console.WriteLine("Use a smaller radius or a larger maximum number of atoms.");
                    logger.Error("The number of selected atoms (" + numAtoms 
                        + ") is larger than the maximum number specified by the argument 4 (" + settings.N + ")! "
                        +"Use a smaller radius or a larger maximum number of atoms.");
                }


                // Closing the streams.

                settings.Input.Close();
                if (settings.Output != Console.Out)
                {
                    settings.Output.Close();
                }
                Program.logger.Close();

            }
            catch (Exception ex)
            {
                if (Program.logger != null)
                {
                    Program.logger.Fatal(ex.Message + ex.StackTrace);
                    Program.logger.CloseFatal();
                }
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
                return;
            }
        }

        public static void SelectResiduesInRadius(State state, Settings settings)
        {
            Program.logger.OpenBlock("Selecting residues in radius by CA.");
            if (!settings.Any)
            {
                state.Builder = new MultiChainBuilder(Lib.ResiduesInRadiusByCA(settings.Protein.GetResidues(), settings.CentralAtom, settings.Radius));
            }
            else
            {
                state.Builder = new MultiChainBuilder(Lib.ResiduesInRadiusByAny(settings.Protein.GetResidues(), settings.CentralAtom, settings.Radius));
            }
            GetLoopsEnds(state, settings);
            Program.logger.CloseBlock("Selecting residues in radius by CA.");
        }

        public static void GetLoopsEnds(State state, Settings settings)
        {
            Program.logger.OpenBlock("Getting loops and ends.");
            
            SortedSet<Residue> residuesAll = settings.Protein.GetResidues();
            SortedSet<Residue> residuesIn = state.Builder.GetResidues();
            SortedSet<Residue> residuesOut = settings.Protein.GetResidues();
            residuesOut.ExceptWith(new SortedSet<Residue>(residuesIn));

            Program.logger.Debug("Residues in residuesAll: " + residuesAll.Count);
            Program.logger.Debug("Residues in residuesIn: " + residuesIn.Count);
            Program.logger.Debug("Residues in residuesOut: " + residuesOut.Count);

            SortedSet<Chain> loopsEndIsolated = new MultiChainBuilder(residuesOut).GetChains();
            state.Loops = new SortedSet<Chain>();
            state.Ends = new SortedSet<Chain>();
            state.Isolated = new SortedSet<Chain>();

            foreach (Chain chain in loopsEndIsolated)
            {
                Residue beginning = chain.GetResidue(chain.FirstResidueNumber);
                Residue end = chain.GetResidue(chain.LastResidueNumber);
                bool beginningTouch = residuesIn.Any(
                    delegate(Residue residue)
                    {
                        return residue.ChainID == beginning.ChainID
                          && residue.ResSeq == beginning.ResSeq - 1;
                    });
                bool endTouch = residuesIn.Any(
                    delegate(Residue residue)
                    {
                        return residue.ChainID == end.ChainID
                            && residue.ResSeq == end.ResSeq + 1;
                    });
                if (beginningTouch && endTouch)
                {
                    state.Loops.Add(chain);
                }
                else if (beginningTouch || endTouch)
                {
                    state.Ends.Add(chain);
                }
                else
                {
                    state.Isolated.Add(chain);
                }
            }

            state.Isolated.RemoveWhere(delegate(Chain chain) { return chain.GetAtoms().Contains(settings.CentralAtom); });
            
            Program.logger.Debug("Found loops, ends and isolated: " + loopsEndIsolated.Count);
            Program.logger.Debug("Found loops: " + state.Loops.Count);
            Program.logger.Debug("Found ends: " + state.Ends.Count);
            Program.logger.Debug("Found isolated: " + state.Isolated.Count);

            Program.logger.CloseBlock("Getting loops and ends.");
        }

        public static void LogSummary(State state, Settings settings, String step)
        {
            Program.logger.OpenBlock("Summary after step " + step);

            SortedSet<Chain> chainFragments = state.Builder.GetChains();
            Program.logger.Info("Number of selected chain fragments: " + chainFragments.Count);
            Program.logger.Info("Number of selected residues: " + Lib.GetResiduesOfAll(chainFragments).Count);
            Program.logger.Info("Number of selected atoms: " + Lib.GetAtomsOfAll(chainFragments).Count);
            Program.logger.Info("Number of backbone cuttings: " + (2 * state.Loops.Count + state.Ends.Count));
            Program.logger.OpenBlock("Numbers of residues in chain fragments: ");
            foreach (Chain chain in chainFragments)
            {
                Program.logger.Info(chain.Count.ToString());
            }
            Program.logger.CloseBlock("Numbers of residues in chain fragments: ");

            Program.logger.Info("Number of loops: " + state.Loops.Count);
            Program.logger.OpenBlock("Numbers of residues in loops: ");
            foreach (Chain chain in state.Loops)
            {
                Program.logger.Info(chain.Count.ToString());
            }
            Program.logger.CloseBlock("Numbers of residues in loops: ");

            Program.logger.Info("Number of ends: " + state.Ends.Count);
            Program.logger.OpenBlock("Numbers of residues in ends: ");
            foreach (Chain chain in state.Ends)
            {
                Program.logger.Info(chain.Count.ToString());
            }
            Program.logger.CloseBlock("Numbers of residues in ends: ");

            Program.logger.Info("Number of isolated fragments: " + state.Isolated.Count);
            Program.logger.OpenBlock("Numbers of residues in isolated fragments: ");
            foreach (Chain chain in state.Isolated)
            {
                Program.logger.Info(chain.Count.ToString());
            }
            Program.logger.CloseBlock("Numbers of residues in isolated fragments: ");

            Program.logger.CloseBlock("Summary after step " + step);

            if (settings.PrintSteps)
            {
                TextWriter writer = (settings.OutputFileName == null) ? Console.Out : new StreamWriter(
                    Path.GetDirectoryName(settings.OutputFileName)
                    + Path.DirectorySeparatorChar
                    + Path.GetFileNameWithoutExtension(settings.OutputFileName) 
                    + "_step_" 
                    + step
                    + Path.GetExtension(settings.OutputFileName));

                foreach (Atom atom in state.Builder.GetAtoms())
                {
                    writer.WriteLine(atom);
                }
                writer.WriteLine(settings.CentralAtom);
                writer.WriteLine();
                writer.Close();

            }
        }

        public static void ExcludeShortFragments(State state,Settings settings)
        {
            Program.logger.OpenBlock("Excluding short chain fragments.");

            SortedSet<Chain> fragments = state.Builder.GetChains();
            MultiChainBuilder newBuilder = new MultiChainBuilder();

            foreach (Chain fragment in fragments)
            {
                if (fragment.Count <= settings.MaxExcludable)
                {
                    Program.logger.Info("A fragment with " + fragment.Count + " residues was excluded.");
                }
                else
                {
                    foreach (Residue residue in fragment.GetResidues())
                    {
                        newBuilder.AddResidue(residue);
                    }
                }
            }

            state.Builder = newBuilder;

            GetLoopsEnds(state, settings);

            Program.logger.CloseBlock("Excluding short chain fragments.");
        }

        public static void AddLoops(State state, Settings settings)
        {
            Program.logger.OpenBlock("Adding loops.");

            SortedSet<Chain> sortedLoops = new SortedSet<Chain>(state.Loops, new Chain.LengthComparer());
            SortedSet<Residue> selectedResidues = state.Builder.GetResidues();
            Program.logger.OpenBlockD("Lengths of sorted loops.");
            foreach (Chain loop in sortedLoops) Program.logger.Debug(loop.Count.ToString());
            Program.logger.CloseBlockD("Lengths of sorted loops.");
            Program.logger.Info(selectedResidues.Sum(delegate(Residue residue) { return residue.GetAtoms().Count; }) + " selected atoms (" + selectedResidues.Count + " residues).");
            while (sortedLoops.Count > 0 &&
                selectedResidues.Sum(delegate(Residue residue) { return residue.GetAtoms().Count; }) + sortedLoops.First().GetAtoms().Count < settings.N)
            {
                Chain shortest = sortedLoops.First();
                Program.logger.Info("Adding a loop with " + shortest.GetAtoms().Count + " atoms (" + shortest.Count + " residues).");
                selectedResidues.UnionWith(shortest.GetResidues());
                sortedLoops.Remove(shortest);
                state.Loops.Remove(shortest);
                Program.logger.Info(selectedResidues.Sum(delegate(Residue residue) { return residue.GetAtoms().Count; }) + " selected atoms (" + selectedResidues.Count + " residues).");
            }
            if (sortedLoops.Count == 0) Program.logger.Info("All loops were added.");
            else Program.logger.Info("Next smallest loop is too big: " + sortedLoops.First().GetAtoms().Count + " atoms (" + sortedLoops.First().Count + " residues).");
            foreach (Residue residue in selectedResidues) state.Builder.AddResidue(residue);

            Program.logger.CloseBlock("Adding loops.");
        }

        public static void AddEnds(State state, Settings settings)
        {
            Program.logger.OpenBlock("Adding ends.");

            SortedSet<Chain> sortedEnds = new SortedSet<Chain>(state.Ends, new Chain.LengthComparer());
            SortedSet<Residue> selectedResidues = state.Builder.GetResidues();
            Program.logger.OpenBlockD("Lengths of sorted ends.");
            foreach (Chain loop in sortedEnds) Program.logger.Debug(loop.Count.ToString());
            Program.logger.CloseBlockD("Lengths of sorted ends.");
            Program.logger.Info(selectedResidues.Sum(delegate(Residue residue) { return residue.GetAtoms().Count; }) + " selected atoms (" + selectedResidues.Count + " residues).");
            while (sortedEnds.Count > 0 &&
                selectedResidues.Sum(delegate(Residue residue) { return residue.GetAtoms().Count; }) + sortedEnds.First().GetAtoms().Count < settings.N)
            {
                Chain shortest = sortedEnds.First();
                Program.logger.Info("Adding an end with " + shortest.GetAtoms().Count + " atoms (" + shortest.Count + " residues).");
                selectedResidues.UnionWith(shortest.GetResidues());
                sortedEnds.Remove(shortest);
                state.Ends.Remove(shortest);
                Program.logger.Info(selectedResidues.Sum(delegate(Residue residue) { return residue.GetAtoms().Count; }) + " selected atoms (" + selectedResidues.Count + " residues).");
            }
            if (sortedEnds.Count == 0) Program.logger.Info("All ends were added.");
            else Program.logger.Info("Next smallest end is too big: " + sortedEnds.First().GetAtoms().Count + " atoms (" + sortedEnds.First().Count + " residues).");
            foreach (Residue residue in selectedResidues) state.Builder.AddResidue(residue);

            Program.logger.CloseBlock("Adding ends.");
        }

        private static void PadToN(State state, Settings settings)
        {
            List<List<Residue>> endsLists = new List<List<Residue>>();
            foreach (Chain end in state.Ends.Union(state.Loops))
            {
                Residue first = end.GetResidues().First();
                Residue last = end.GetResidues().Last();

                if (state.Builder.GetResidues().Any(delegate(Residue res)
                {
                    return res.ChainID == first.ChainID && res.ResSeq == first.ResSeq - 1;
                }))
                {
                    endsLists.Add(end.GetResidues().ToList());
                }
                if (state.Builder.GetResidues().Any(delegate(Residue res)
                {
                    return res.ChainID == last.ChainID && res.ResSeq == last.ResSeq + 1;
                }))
                {
                    List<Residue> list = end.GetResidues().ToList();
                    list.Reverse();
                    endsLists.Add(list);
                }
            }
            List<Residue> residues = Lib.Merge<Residue>(endsLists, new Residue.DistanceComparer(settings.CentralAtom));
            int m = state.Builder.GetAtoms().Count;
            while (residues.Count > 0 && m + residues.First().GetAtoms().Count <= settings.N)
            {
                Residue first = residues.First();
                state.Builder.AddResidue(first);
                m += first.GetAtoms().Count;
                residues.Remove(first);
            }
            GetLoopsEnds(state, settings);
        }

        private static SortedSet<Atom> RunAlgorithm(State state, Settings settings)
        {
            switch (settings.Mode)
            {
                case 1:
                    SelectResiduesInRadius(state, settings);
                    LogSummary(state, settings, "1");
                    ExcludeShortFragments(state, settings);
                    LogSummary(state, settings, "2");
                    AddLoops(state, settings);
                    LogSummary(state, settings, "3");
                    AddEnds(state, settings);
                    LogSummary(state, settings, "4");
                    PadToN(state, settings);
                    if (settings.LogMode >= 1) Program.logger.TotalIgnore = false;
                    LogSummary(state, settings, "5");
                    break;
                case 2:
                    SelectResiduesInRadius(state, settings);
                    LogSummary(state, settings, "1");
                    AddLoops(state, settings);
                    LogSummary(state, settings, "2");
                    ExcludeShortFragments(state, settings);
                    LogSummary(state, settings, "3");
                    AddEnds(state, settings);
                    LogSummary(state, settings, "4");
                    PadToN(state, settings);
                    if (settings.LogMode >= 1) Program.logger.TotalIgnore = false;
                    LogSummary(state, settings, "5");
                    break;
                case 3:
                    SelectResiduesInRadius(state, settings);
                    LogSummary(state, settings, "1");
                    AddLoops(state, settings);
                    LogSummary(state, settings, "2");
                    AddEnds(state, settings);
                    LogSummary(state, settings, "3");
                    ExcludeShortFragments(state, settings);
                    LogSummary(state, settings, "4");
                    PadToN(state, settings);
                    if (settings.LogMode >= 1) Program.logger.TotalIgnore = false;
                    LogSummary(state, settings, "5");
                    break;
                case 4:
                    SelectResiduesInRadius(state, settings);
                    LogSummary(state, settings, "1");
                    AddLoops(state, settings);
                    LogSummary(state, settings, "2");
                    ExcludeShortFragments(state, settings);
                    LogSummary(state, settings, "3");
                    AddLoops(state, settings);
                    LogSummary(state, settings, "4");
                    AddEnds(state, settings);
                    LogSummary(state, settings, "5");
                    PadToN(state, settings);
                    if (settings.LogMode >= 1) Program.logger.TotalIgnore = false;
                    LogSummary(state, settings, "6");
                    break;
                case 5:
                    SelectResiduesInRadius(state, settings);
                    LogSummary(state, settings, "1");
                    AddLoops(state, settings);
                    LogSummary(state, settings, "2");
                    AddEnds(state, settings);
                    LogSummary(state, settings, "3");
                    ExcludeShortFragments(state, settings);
                    LogSummary(state, settings, "4");
                    AddLoops(state, settings);
                    LogSummary(state, settings, "5");
                    AddEnds(state, settings);
                    LogSummary(state, settings, "6");
                    PadToN(state, settings);
                    if (settings.LogMode >= 1) Program.logger.TotalIgnore = false;
                    LogSummary(state, settings, "7");
                    break;
            }

            SortedSet<Atom> selectedAtoms = Lib.GetAtomsOfAll(state.Builder.GetResidues());
            selectedAtoms.Add(settings.CentralAtom);
            return selectedAtoms;
        }

        private static void RunPyMOLScript(Settings settings)
        {
            string pymolExe = AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + "PyMOL.exe";
            if (!File.Exists(pymolExe))
            {
                Console.WriteLine("Warning: PyMOL not found, no session created.");
                Console.WriteLine("PyMOL.exe must be located in the same directory as FragmentSelector.exe.");
                logger.Error("Warning: PyMOL not found, no session created. "
                    + "PyMOL.exe must be located in the same directory as FragmentSelector.exe.");
                return;
            }
            
            StringBuilder script=null;
            if (settings.PrintSteps)
            {
                switch (settings.Mode)
                {
                    case 1:
                        script = new StringBuilder(BP01.Properties.Resources.script_PyMOL_steps_m1);
                        break;
                    case 2:
                        script = new StringBuilder(BP01.Properties.Resources.script_PyMOL_steps_m2);
                        break;
                    case 3:
                        script = new StringBuilder(BP01.Properties.Resources.script_PyMOL_steps_m3);
                        break;
                    case 4:
                        script = new StringBuilder(BP01.Properties.Resources.script_PyMOL_steps_m4);
                        break;
                    case 5:
                        script = new StringBuilder(BP01.Properties.Resources.script_PyMOL_steps_m5);
                        break;
                }
            }
            else
            {
                script = new StringBuilder(BP01.Properties.Resources.script_PyMOL);
            }

            script = script
                .Replace("${inputFile}",
                    settings.InputFileName)
                .Replace("${outputFileWithoutExtension}",
                    Path.GetDirectoryName(settings.OutputFileName)
                    + Path.DirectorySeparatorChar
                    + Path.GetFileNameWithoutExtension(settings.OutputFileName))
                .Replace("${outputFileExtension}",
                    Path.GetExtension(settings.OutputFileName))
                .Replace("${centralAtomSymbol}",
                    settings.CentralAtom.Element);
            if (settings.QuitAfterScript)
            {
                script.Append(" quit;");
            }

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = pymolExe;
            startInfo.Arguments =
                settings.ScriptInCommandLineMode ?
                "-qc -d \"" + script + "\""
                : "-d \"" + script + "\"";
            Process.Start(startInfo);
        }

    }
}
