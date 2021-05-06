using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace FragmentSelector {
    static class Lib {
        /**
         * Calculates the distance between two given atoms.
         */
        public static double Distance(Atom a1, Atom a2) {
            return Math.Sqrt(Math.Pow(a1.X - a2.X, 2) + Math.Pow(a1.Y - a2.Y, 2) + Math.Pow(a1.Z - a2.Z, 2));
        }

        /**
         * Selects all atoms which are closer to central atom than radius.
         */
        public static SortedSet<Atom> InRadius(SortedSet<Atom> atoms, Atom centralAtom, double radius) {
            SortedSet<Atom> inRadius = new SortedSet<Atom>();
            foreach (Atom atom in atoms) {
                if (Distance(atom, centralAtom) <= radius) {
                    inRadius.Add(atom);
                }
            }
            return inRadius;
        }

        /**
         * Selects all residues that have any atom closer to central atom than radius.
         */
        public static SortedSet<Residue> ResiduesInRadiusByAny(ICollection<Residue> residues, Atom centralAtom, double radius) {
            return new SortedSet<Residue>(residues.Where(
                delegate (Residue res) {
                    return res.GetAtoms().Any(
                        delegate (Atom at) {
                            return Distance(at, centralAtom) <= radius;
                        });
                }));
        }
        /**
         * Selects all residues whose C alpha atom is closer to central atom than radius.
         */
        public static SortedSet<Residue> ResiduesInRadiusByCA(ICollection<Residue> residues, Atom centralAtom, double radius) {
            return new SortedSet<Residue>(residues.Where(res => res.GetAtoms().Any(at => at.Name.Equals(" CA ") && Distance(at, centralAtom) <= radius)));
        }

        /**
         * If byAnyAtom==false, selects all residues whose C alpha atom is closer to any central atom than radius, 
         * if byAnyAtom==true, select all residues whose any atom is closer to any central atom than radius, 
         */
        public static SortedSet<Residue> ResiduesInRadius(ICollection<Residue> residues, ICollection<Atom> centralAtoms, double radius, bool byAnyAtom) {
            SortedSet<Residue> result = new SortedSet<Residue>();
            foreach (Residue residue in residues){
                Atom[] resAtoms = byAnyAtom ? residue.GetAtoms().ToArray() : residue.GetAtoms().Where(a => a.Name.Trim() == "CA").ToArray();
                bool inRadius = resAtoms.Any(ra => centralAtoms.Any(ca => Distance(ra, ca) <= radius));
                if (inRadius){
                    result.Add(residue);
                }
            }
            return result;
        }

        public static SortedSet<Atom> GetAtomsOfAll(ICollection<Chain> chains) {
            SortedSet<Atom> result = new SortedSet<Atom>();
            foreach (Chain chain in chains) {
                result.UnionWith(chain.GetAtoms());
            }
            return result;
        }

        public static SortedSet<Atom> GetAtomsOfAll(ICollection<Residue> residues) {
            SortedSet<Atom> result = new SortedSet<Atom>();
            foreach (Residue residue in residues) {
                result.UnionWith(residue.GetAtoms());
            }
            return result;
        }

        public static SortedSet<Residue> GetResiduesOfAll(ICollection<Chain> chains) {
            SortedSet<Residue> result = new SortedSet<Residue>();
            foreach (Chain chain in chains) {
                result.UnionWith(chain.GetResidues());
            }
            return result;
        }

        public static List<T> Merge<T>(List<List<T>> lists, IComparer<T> comparer) {
            List<T> result = new List<T>();
            List<T> next;
            do {
                next = null;
                foreach (List<T> list in lists) {
                    if (list.Count != 0) {
                        if (next == null || comparer.Compare(list.First(), next.First()) < 0) {
                            next = list;
                        }
                    }
                }
                if (next != null) {
                    result.Add(next.First());
                    next.RemoveAt(0);
                }
            } while (next != null);
            return result;
        }

        public static void DoForAllFiles(String directory, Action<String> action) {
            if (!Directory.Exists(directory)) return;
            foreach (String file in Directory.GetFiles(directory)) {
                action(file);
            }
            foreach (String dir in Directory.GetDirectories(directory)) {
                DoForAllFiles(dir, action);
            }
        }

        public static void DoForAllFiles(String directory, Action<String> action, String regex) {
            DoForAllFiles(directory, delegate (String file) {
                String fileName = Path.GetFileName(file);
                if (Regex.IsMatch(fileName, regex)) action(file);
            });
        }


        /** Runs a command with specified arguments. */
        public static bool RunCommand(String command, String args) {
            bool rooted = Path.IsPathRooted(command);
            if (rooted) {
                if (!File.Exists(command)) {
                    Console.WriteLine("File '{0}' not found.", command);
                    return false;
                }
            } else {
                String commandHere = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, command);
                if (File.Exists(commandHere)) {
                    Console.WriteLine($"Info: File '{command}; found in the base directory '{AppDomain.CurrentDomain.BaseDirectory}'");
                    command = commandHere;
                } else {
                    Console.WriteLine($"Info: File '{command}' not found in the base directory '{AppDomain.CurrentDomain.BaseDirectory}'.");
                    Console.WriteLine($"      Running '{command}' from current directory or $PATH.");
                }
            }
            try {
                Process proc = new Process();
                proc.StartInfo = new ProcessStartInfo();
                proc.StartInfo.FileName = command;
                proc.StartInfo.Arguments = args;
                proc.StartInfo.UseShellExecute = false; // without this it throws something about xdg-open on Linux if the pymol is not installed
                Console.WriteLine("Executing: ");
                Console.WriteLine("    " + command + " " + args);
                proc.Start();
                proc.WaitForExit();
                int exitCode = proc.ExitCode;
                bool success = exitCode == 0;
                String successString = success ? "success" : "fail";
                Console.WriteLine($"Exit code: {exitCode} ({successString})\n");
                return success;
            } catch (System.ComponentModel.Win32Exception e) {
                Console.WriteLine($"Could not run: {command}");
                Console.WriteLine($"Win32Exception: {e.Message}\n");
                throw e;
                return false;
            }
        }

    }
}
