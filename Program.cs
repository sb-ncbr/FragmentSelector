using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace FragmentSelector {
    class Program {
        public static Logger logger;
        const string VERSION = "FragmentSelector 2.0 [2021-03-18]";

        static int Main(string[] args) {
            try {
                // Declaration of variables.
                Settings settings = new Settings();
                settings.MaxExcludable = 0;
                settings.PrintSteps = false;
                settings.OutputFileName = null;
                settings.LogMode = 2; // 0 = no logging, 1 = just result, 2 = all
                settings.Mode = 3;
                // 1 = Select - Exclude short fragments - Add loops - Add ends - Pad to N
                // 2 = Select - Add loops - Exclude short fragments - Add ends - Pad to N
                // 3 = Select - Add loops - Add ends - Exclude short fragments - Pad to N
                // 4 = Select - Add loops - Exclude short fragments - Add loops - Add ends - Pad to N atoms
                // 5 = Select - Add loops - Add ends - Exclude short fragments - Add loops
                settings.RunScript = false;
                settings.Any = false;

                // Reading arguments and options.
                bool argsOK = TryReadArguments(args, settings);
                if (!argsOK) {
                    return 1;
                }

                // Creating the log file.
                if (settings.LogMode >= 1) {
                    if (settings.OutputFileName != null) {
                        String logFileName = Path.GetDirectoryName(settings.OutputFileName)
                            + Path.DirectorySeparatorChar
                            + Path.GetFileNameWithoutExtension(settings.OutputFileName) + "_log.xml";
                        Program.logger = new Logger(new StreamWriter(logFileName), Logger.Level.INFO);
                    } else {
                        Program.logger = new Logger(Console.Error);
                    }
                    if (settings.LogMode == 1) Program.logger.TotalIgnore = true;
                } else {
                    Program.logger = new Logger(Console.Error, true);
                }

                // Logging the settings.
                if (settings.LogMode == 1) Program.logger.TotalIgnore = false;
                Program.logger.OpenBlock("Settings");
                Program.logger.Info("Input file = \"" + settings.InputFileName + "\"");
                // Program.logger.Info("Index of the central atom = " + settings.CentralAtomIndex);
                Program.logger.Info("Central atom query = " + settings.CentralAtomQuery);
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
                settings.Protein = ReadProtein(settings.InputFileName);
                if (settings.Protein == null){
                    return 2;
                }
                Program.logger.Info(settings.Protein.GetChains().Count + " chains found.");
                Program.logger.Info(settings.Protein.GetResidues().Count + " residues found.");
                Program.logger.Info(settings.Protein.GetAtoms().Count + " atoms found.");
                Program.logger.CloseBlock("Parsing the input file.");

                // Finding the central atom.
                settings.CentralAtoms = settings.CentralAtomQuery.FindInProtein(settings.Protein);
                Program.logger.Info($"Central atoms: {settings.CentralAtoms.Length}");
                Console.WriteLine($"Central atoms: {settings.CentralAtoms.Length} ({settings.CentralAtomQuery})");
                if (settings.CentralAtoms.Length == 0){
                    Console.Error.WriteLine("Error: No central atoms.");
                    Program.logger.Fatal("No central atoms.");
                    Program.logger.CloseFatal();
                    return 3;
                }
                // Removing residues with no C alpha atom.
                settings.Protein = settings.Protein.KeepOnlyNormalResidues();

                // Selecting atoms for output.
                State state = new State();
                SortedSet<Atom> selectedAtoms = RunAlgorithm(state, settings);

                // Output.
                bool writeOK = WriteAtoms(selectedAtoms, settings.OutputFileName);
                if (!writeOK){
                    return 4;
                }

                // Running a script for PyMOL.
                if (settings.RunScript) {
                    RunPyMOLScript(settings);
                }

                // Writting summary on standard output.
                SortedSet<Chain> chainFragments = state.Builder.GetChains();
                Console.WriteLine("Number of chain fragments: " + chainFragments.Count);
                Console.WriteLine("Number of loops: " + state.Loops.Count);
                Console.WriteLine("Number of ends: " + state.Ends.Count);
                Console.WriteLine("Number of isolated fragments: " + state.Isolated.Count);
                Console.WriteLine("Number of selected residues: " + Lib.GetResiduesOfAll(chainFragments).Count);
                int numAtoms = Lib.GetAtomsOfAll(chainFragments).Count;
                Console.WriteLine("Number of selected atoms: " + numAtoms);
                Console.WriteLine("Number of backbone cuttings: " + (2 * state.Loops.Count + state.Ends.Count));
                if (numAtoms > settings.N) {
                    Console.WriteLine("Warning: the number of selected atoms (" + numAtoms
                        + ") is larger than the maximum number specified by the parameter 4 (" + settings.N + ")!");
                    Console.WriteLine("Use a smaller radius or a larger maximum number of atoms.");
                    logger.Error("The number of selected atoms (" + numAtoms
                        + ") is larger than the maximum number specified by the argument 4 (" + settings.N + ")! "
                        + "Use a smaller radius or a larger maximum number of atoms.");
                }
                // Closing the log stream.
                Program.logger.Close();
                return 0;

            } catch (Exception ex) {
                if (Program.logger != null) {
                    Program.logger.Fatal(ex.Message + ex.StackTrace);
                    Program.logger.CloseFatal();
                }
                Console.Error.WriteLine(ex);
                Console.Error.WriteLine(ex.StackTrace);
                // throw ex;
                return -1;
            }
        }

        public static void SelectResiduesInRadius(State state, Settings settings) {
            string blockName = settings.Any ? "Selecting residues in radius by any atom." : "Selecting residues in radius by CA.";
            Program.logger.OpenBlock(blockName);
            state.Builder = new MultiChainBuilder(Lib.ResiduesInRadius(settings.Protein.GetResidues(), settings.CentralAtoms, settings.Radius, settings.Any));
            // if (!settings.Any) {
            //     state.Builder = new MultiChainBuilder(Lib.ResiduesInRadiusByCA(settings.Protein.GetResidues(), settings.CentralAtom, settings.Radius));
            // } else {
            //     state.Builder = new MultiChainBuilder(Lib.ResiduesInRadiusByAny(settings.Protein.GetResidues(), settings.CentralAtom, settings.Radius));
            // }
            GetLoopsEnds(state, settings);
            Program.logger.CloseBlock(blockName);
        }

        public static void GetLoopsEnds(State state, Settings settings) {
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

            foreach (Chain chain in loopsEndIsolated) {
                Residue beginning = chain.GetResidue(chain.FirstResidueNumber);
                Residue end = chain.GetResidue(chain.LastResidueNumber);
                bool beginningTouch = residuesIn.Any(
                    delegate (Residue residue) {
                        return residue.ChainID == beginning.ChainID
                          && residue.ResSeq == beginning.ResSeq - 1;
                    });
                bool endTouch = residuesIn.Any(
                    delegate (Residue residue) {
                        return residue.ChainID == end.ChainID
                            && residue.ResSeq == end.ResSeq + 1;
                    });
                if (beginningTouch && endTouch) {
                    state.Loops.Add(chain);
                } else if (beginningTouch || endTouch) {
                    state.Ends.Add(chain);
                } else {
                    state.Isolated.Add(chain);
                }
            }

            // state.Isolated.RemoveWhere(delegate (Chain chain) { return chain.GetAtoms().Contains(settings.CentralAtom); });
            // state.Isolated.RemoveWhere(chain => chain.GetAtoms().Contains(settings.CentralAtom));
            state.Isolated.RemoveWhere(chain => chain.ContainsAny(settings.CentralAtoms));

            Program.logger.Debug("Found loops, ends and isolated: " + loopsEndIsolated.Count);
            Program.logger.Debug("Found loops: " + state.Loops.Count);
            Program.logger.Debug("Found ends: " + state.Ends.Count);
            Program.logger.Debug("Found isolated: " + state.Isolated.Count);

            Program.logger.CloseBlock("Getting loops and ends.");
        }

        public static void LogSummary(State state, Settings settings, String step) {
            Program.logger.OpenBlock("Summary after step " + step);

            SortedSet<Chain> chainFragments = state.Builder.GetChains();
            Program.logger.Info("Number of selected chain fragments: " + chainFragments.Count);
            Program.logger.Info("Number of selected residues: " + Lib.GetResiduesOfAll(chainFragments).Count);
            Program.logger.Info("Number of selected atoms: " + Lib.GetAtomsOfAll(chainFragments).Count);
            Program.logger.Info("Number of backbone cuttings: " + (2 * state.Loops.Count + state.Ends.Count));
            Program.logger.OpenBlock("Numbers of residues in chain fragments: ");
            foreach (Chain chain in chainFragments) {
                Program.logger.Info(chain.Count.ToString());
            }
            Program.logger.CloseBlock("Numbers of residues in chain fragments: ");

            Program.logger.Info("Number of loops: " + state.Loops.Count);
            Program.logger.OpenBlock("Numbers of residues in loops: ");
            foreach (Chain chain in state.Loops) {
                Program.logger.Info(chain.Count.ToString());
            }
            Program.logger.CloseBlock("Numbers of residues in loops: ");

            Program.logger.Info("Number of ends: " + state.Ends.Count);
            Program.logger.OpenBlock("Numbers of residues in ends: ");
            foreach (Chain chain in state.Ends) {
                Program.logger.Info(chain.Count.ToString());
            }
            Program.logger.CloseBlock("Numbers of residues in ends: ");

            Program.logger.Info("Number of isolated fragments: " + state.Isolated.Count);
            Program.logger.OpenBlock("Numbers of residues in isolated fragments: ");
            foreach (Chain chain in state.Isolated) {
                Program.logger.Info(chain.Count.ToString());
            }
            Program.logger.CloseBlock("Numbers of residues in isolated fragments: ");

            Program.logger.CloseBlock("Summary after step " + step);

            if (settings.PrintSteps) {
                TextWriter writer = (settings.OutputFileName == null) ? Console.Out : new StreamWriter(
                    Path.GetDirectoryName(settings.OutputFileName)
                    + Path.DirectorySeparatorChar
                    + Path.GetFileNameWithoutExtension(settings.OutputFileName)
                    + "_step_"
                    + step
                    + Path.GetExtension(settings.OutputFileName));

                foreach (Atom atom in settings.CentralAtoms) {
                    writer.WriteLine(atom);
                }
                foreach (Atom atom in state.Builder.GetAtoms()) {
                    writer.WriteLine(atom);
                }
                // writer.WriteLine(settings.CentralAtom);
                writer.WriteLine();
                writer.Close();

            }
        }

        public static void ExcludeShortFragments(State state, Settings settings) {
            Program.logger.OpenBlock("Excluding short chain fragments.");

            SortedSet<Chain> fragments = state.Builder.GetChains();
            MultiChainBuilder newBuilder = new MultiChainBuilder();

            foreach (Chain fragment in fragments) {
                if (fragment.Count <= settings.MaxExcludable) {
                    Program.logger.Info("A fragment with " + fragment.Count + " residues was excluded.");
                } else {
                    foreach (Residue residue in fragment.GetResidues()) {
                        newBuilder.AddResidue(residue);
                    }
                }
            }

            state.Builder = newBuilder;

            GetLoopsEnds(state, settings);

            Program.logger.CloseBlock("Excluding short chain fragments.");
        }

        public static void AddLoops(State state, Settings settings) {
            Program.logger.OpenBlock("Adding loops.");

            SortedSet<Chain> sortedLoops = new SortedSet<Chain>(state.Loops, new Chain.LengthComparer());
            SortedSet<Residue> selectedResidues = state.Builder.GetResidues();
            Program.logger.OpenBlockD("Lengths of sorted loops.");
            foreach (Chain loop in sortedLoops) Program.logger.Debug(loop.Count.ToString());
            Program.logger.CloseBlockD("Lengths of sorted loops.");
            Program.logger.Info(selectedResidues.Sum(delegate (Residue residue) { return residue.GetAtoms().Count; }) + " selected atoms (" + selectedResidues.Count + " residues).");
            while (sortedLoops.Count > 0 &&
                    selectedResidues.Sum(residue => residue.GetAtoms().Count) + sortedLoops.First().GetAtoms().Count + settings.CentralAtoms.Length <= settings.N) {
                Chain shortest = sortedLoops.First();
                Program.logger.Info("Adding a loop with " + shortest.GetAtoms().Count + " atoms (" + shortest.Count + " residues).");
                selectedResidues.UnionWith(shortest.GetResidues());
                sortedLoops.Remove(shortest);
                state.Loops.Remove(shortest);
                Program.logger.Info(selectedResidues.Sum(delegate (Residue residue) { return residue.GetAtoms().Count; }) + " selected atoms (" + selectedResidues.Count + " residues).");
            }
            if (sortedLoops.Count == 0) Program.logger.Info("All loops were added.");
            else Program.logger.Info("Next smallest loop is too big: " + sortedLoops.First().GetAtoms().Count + " atoms (" + sortedLoops.First().Count + " residues).");
            foreach (Residue residue in selectedResidues) state.Builder.AddResidue(residue);

            Program.logger.CloseBlock("Adding loops.");
        }

        public static void AddEnds(State state, Settings settings) {
            Program.logger.OpenBlock("Adding ends.");

            SortedSet<Chain> sortedEnds = new SortedSet<Chain>(state.Ends, new Chain.LengthComparer());
            SortedSet<Residue> selectedResidues = state.Builder.GetResidues();
            Program.logger.OpenBlockD("Lengths of sorted ends.");
            foreach (Chain loop in sortedEnds) Program.logger.Debug(loop.Count.ToString());
            Program.logger.CloseBlockD("Lengths of sorted ends.");
            Program.logger.Info(selectedResidues.Sum(residue => residue.GetAtoms().Count) + " selected atoms (" + selectedResidues.Count + " residues).");
            while (sortedEnds.Count > 0 &&
                selectedResidues.Sum(residue => residue.GetAtoms().Count) + sortedEnds.First().GetAtoms().Count + settings.CentralAtoms.Length <= settings.N) {
                Chain shortest = sortedEnds.First();
                Program.logger.Info("Adding an end with " + shortest.GetAtoms().Count + " atoms (" + shortest.Count + " residues).");
                selectedResidues.UnionWith(shortest.GetResidues());
                sortedEnds.Remove(shortest);
                state.Ends.Remove(shortest);
                Program.logger.Info(selectedResidues.Sum(delegate (Residue residue) { return residue.GetAtoms().Count; }) + " selected atoms (" + selectedResidues.Count + " residues).");
            }
            if (sortedEnds.Count == 0) Program.logger.Info("All ends were added.");
            else Program.logger.Info("Next smallest end is too big: " + sortedEnds.First().GetAtoms().Count + " atoms (" + sortedEnds.First().Count + " residues).");
            foreach (Residue residue in selectedResidues) state.Builder.AddResidue(residue);

            Program.logger.CloseBlock("Adding ends.");
        }

        private static void PadToN(State state, Settings settings) {
            List<List<Residue>> endsLists = new List<List<Residue>>();
            foreach (Chain end in state.Ends.Union(state.Loops)) {
                Residue first = end.GetResidues().First();
                Residue last = end.GetResidues().Last();

                if (state.Builder.GetResidues().Any(delegate (Residue res) {
                    return res.ChainID == first.ChainID && res.ResSeq == first.ResSeq - 1;
                })) {
                    endsLists.Add(end.GetResidues().ToList());
                }
                if (state.Builder.GetResidues().Any(delegate (Residue res) {
                    return res.ChainID == last.ChainID && res.ResSeq == last.ResSeq + 1;
                })) {
                    List<Residue> list = end.GetResidues().ToList();
                    list.Reverse();
                    endsLists.Add(list);
                }
            }
            List<Residue> residues = Lib.Merge<Residue>(endsLists, new Residue.DistanceComparer(settings.CentralAtoms));
            int m = state.Builder.GetAtoms().Count;
            while (residues.Count > 0 && m + residues.First().GetAtoms().Count + settings.CentralAtoms.Length <= settings.N) {
                Residue first = residues.First();
                state.Builder.AddResidue(first);
                m += first.GetAtoms().Count;
                residues.Remove(first);
            }
            GetLoopsEnds(state, settings);
        }

        private static SortedSet<Atom> RunAlgorithm(State state, Settings settings) {
            switch (settings.Mode) {
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
            // selectedAtoms.Add(settings.CentralAtom);
            selectedAtoms.UnionWith(settings.CentralAtoms);
            return selectedAtoms;
        }

        private static bool RunPyMOLScript(Settings settings) {
            string configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.txt");
            Console.WriteLine(configFile);
            Config config;
            try {
                config = new Config(configFile);
            } catch (FileNotFoundException) {
                Console.Error.WriteLine("Warning: configuration file not found, no PyMOL session created.");
                Console.Error.WriteLine("    Configuration file must be named config.txt and located in the same directory as FragmentSelector.dll");
                Console.Error.WriteLine("    Example config file:");
                Console.Error.WriteLine("        # Set path to PyMOL executable or simply 'pymol' if it is on PATH:");
                Console.Error.WriteLine("        PymolExecutable: C:/ProgramData/PyMOL/Scripts/pymol.exe");
                Console.Error.WriteLine("        # Set path to PyMOL script (absolute or relative to this config file):");
                Console.Error.WriteLine("        PymolScript: ./script_pymol.py");
                return false;
            }

            string scriptFilename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, config.PymolScript);
            Console.WriteLine(scriptFilename);

            string mode = settings.PrintSteps ? settings.Mode.ToString() : "0";
            string outputFileWithoutExt = Path.Combine(Path.GetDirectoryName(settings.OutputFileName), Path.GetFileNameWithoutExtension(settings.OutputFileName));
            string outputFileExt = Path.GetExtension(settings.OutputFileName);
            string centralAtomString = string.Join('+', settings.CentralAtoms.Select(a => a.Serial.ToString()));
            string[] args = new string[] {"-qcyr", Escape(scriptFilename), "--", 
                Escape(mode), Escape(settings.InputFileName), Escape(outputFileWithoutExt), Escape(outputFileExt), Escape(centralAtomString) };
            string arguments = string.Join(' ', args);

            bool success = Lib.RunCommand(config.PymolExecutable, arguments);
            return success;
        }

        private static string Escape(string path){
            char QUOTE = '"';
            return QUOTE + path + QUOTE;
        }

        private static bool TryReadArguments(string[] args, Settings settings) {
            int iArg = 0;
            List<string> regularArgs = new List<string>();

            // Processing the options.
            while (iArg < args.Length) {
                if (args[iArg].StartsWith("-")) {
                    switch (args[iArg]) {
                        case "-a":
                            settings.Any = true;
                            iArg++;
                            break;
                        case "-e":
                            if (iArg + 1 >= args.Length || args[iArg + 1].StartsWith('-')) {
                                Console.Error.WriteLine("Option \"-e\" requires an argument.");
                                return false;
                            }
                            int me;
                            if (Int32.TryParse(args[iArg + 1], out me)) {
                                settings.MaxExcludable = me;
                            } else {
                                Console.Error.WriteLine("Wrong format of argument of \"-e\": integer expected, \"" + args[iArg + 1] + "\" found.");
                                return false;
                            }
                            iArg += 2;
                            break;
                        case "-l":
                            if (iArg + 1 >= args.Length || args[iArg + 1].StartsWith('-')) {
                                Console.Error.WriteLine("Option \"-l\" requires an argument.");
                                return false;
                            }
                            int lm;
                            if (Int32.TryParse(args[iArg + 1], out lm) && Settings.LOG_MODES.Contains(lm)) {
                                settings.LogMode = lm;
                            } else {
                                Console.Error.WriteLine("Wrong format of argument of \"-l\": integer from 0 to 2 expected, \"" + args[iArg + 1] + "\" found.");
                                return false;
                            }
                            iArg += 2;
                            break;
                        case "-m":
                            if (iArg + 1 >= args.Length || args[iArg + 1].StartsWith('-')) {
                                Console.Error.WriteLine("Option \"-l\" requires an argument.");
                                return false;
                            }
                            int m;
                            if (Int32.TryParse(args[iArg + 1], out m) && Settings.MODES.Contains(m)) {
                                settings.Mode = m;
                            } else {
                                Console.Error.WriteLine("Wrong format of argument of \"-m\": integer from 1 to 5 expected, \"" + args[iArg + 1] + "\" found.");
                                return false;
                            }
                            iArg += 2;
                            break;
                        case "-o":
                            if (iArg + 1 >= args.Length || args[iArg + 1].StartsWith('-')) {
                                Console.Error.WriteLine("Option \"-o\" requires an argument.");
                                return false;
                            }
                            settings.OutputFileName = args[iArg + 1];
                            iArg += 2;
                            break;
                        case "-r":
                            settings.RunScript = true;
                            iArg += 1;
                            break;
                        case "-s":
                            settings.PrintSteps = true;
                            iArg += 1;
                            break;
                        default:
                            Console.Error.WriteLine("Unknown option: \"" + args[iArg] + "\"");
                            return false;
                    }
                } else {
                    regularArgs.Add(args[iArg]);
                    iArg += 1;
                }
            }


            // Processing the arguments.
            if (regularArgs.Count != 4) {
                PrintHelp(regularArgs.Count);
                return false;
            }
            settings.InputFileName = regularArgs[0];

            try {
                settings.CentralAtomQuery = new AtomQuery(regularArgs[1]);
            } catch (FormatException ex) {
                Console.Error.WriteLine($"Wrong format of 2nd argument: {ex.Message}");
                return false;
            }
            // int cai;
            // if (Int32.TryParse(regularArgs[1], out cai)) {
            //     settings.CentralAtomIndex = cai;
            // } else {
            //     Console.Error.WriteLine("Wrong format of 2nd argument: integer expected.");
            //     return false;
            // }

            double r;
            if (Double.TryParse(regularArgs[2], NumberStyles.Float, new CultureInfo("en-US"), out r)) {
                settings.Radius = r;
            } else {
                Console.Error.WriteLine("Wrong format of 3rd argument: real number expected.");
                return false;
            }

            int n;
            if (Int32.TryParse(regularArgs[3], out n)) {
                settings.N = n;
            } else {
                Console.Error.WriteLine("Wrong format of 4th argument: integer expected.");
                return false;
            }

            return true;
        }

        private static void ParseAtomQuery(){}

        private static void PrintHelp(int nActualArgs) {
            Console.Error.WriteLine(VERSION);
            Console.Error.WriteLine();
            Console.Error.WriteLine("Wrong number or type of arguments: 4 expected, " + nActualArgs + " found.");
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
            Console.Error.WriteLine("   -s                   Prints result of each step in a separate file.");
        }

        private static Protein ReadProtein(string filename){
            StreamReader inputReader;
            try {
                inputReader = new StreamReader(filename);
            } catch (ArgumentException) {
                Console.Error.WriteLine("File name is empty string.");
                return null;
            } catch (FileNotFoundException) {
                Console.Error.WriteLine("File \"" + filename + "\" not found.");
                return null;
            } catch (DirectoryNotFoundException) {
                Console.Error.WriteLine("Directory not found.");
                return null;
            } catch (IOException) {
                Console.Error.WriteLine("IOException while accessing file " + filename + ".");
                return null;
            }

            Protein protein = new Protein(inputReader);
            inputReader.Close();
            return protein;
        }

        private static bool WriteAtoms(IEnumerable<Atom> atoms, string filename){
            TextWriter writer;
            if (filename != null) {
                try {
                    writer = new StreamWriter(filename);
                } catch (IOException ex) {
                    Console.Error.WriteLine("Cannot write to file \"" + filename + "\".");
                    return false;
                }
            } else {
                writer = Console.Out;
            }

            foreach (Atom atom in atoms) {
                writer.WriteLine(atom);
            }
            writer.WriteLine();

            if (writer != Console.Out) {
                writer.Close();
            }
            return true;
        }

    }
}
