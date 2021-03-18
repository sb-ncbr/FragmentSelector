using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BP01
{
    class Protein
    {
        private Dictionary<char,Chain> chains;

        public Protein()
        {
            chains = new Dictionary<char, Chain>();
        }

        public Protein(StreamReader reader)
        {
            chains = new Dictionary<char, Chain>();

            String line;
            do
            {
                try
                {
                    line = reader.ReadLine();
                }
                catch (IOException)
                {
                    Console.WriteLine("IOException while reading from stream.");
                    return;
                }
                catch (OutOfMemoryException)
                {
                    Console.WriteLine("OutOfMemoryException.");
                    return;
                }

                if (line != null)
                {
                    if (line.Length >= 6 && line.Substring(0, 6).Equals("ATOM  "))
                    {
                        Atom atom = new Atom(line);
                        this.AddAtom(atom);
                    }
                    else if (line.Length >= 6 && line.Substring(0, 6).Equals("HETATM"))
                    {
                        Atom atom = new Atom(line);
                        this.AddAtom(atom);
                    }
                }
            } while (line != null);
        }

        /**
         * Returns a protein containing only those residues which have a C alpha atom.
         */
        public Protein KeepOnlyNormalResidues()
        {
            Protein result = new Protein();
            foreach (Residue res in GetResidues())
            {
                if (res.GetAtoms().Any(delegate(Atom a) { return a.Name == " CA "; }))
                {
                    result.AddResidue(res);
                }
            }
            return result;
        }

        public SortedSet<Chain> GetChains()
        {
            return new SortedSet<Chain>(chains.Values);
        }
        
        public SortedSet<Residue> GetResidues()
        {
            SortedSet<Residue> result = new SortedSet<Residue>();
            foreach (Chain chain in this.GetChains())
            {
                result.UnionWith(chain.GetResidues());
            }
            return result;
        }
        
        public SortedSet<Atom> GetAtoms()
        {
            SortedSet<Atom> result = new SortedSet<Atom>();
            foreach (Residue residue in this.GetResidues())
            {
                result.UnionWith(residue.GetAtoms());
            }
            return result;
        }
        
        public Chain GetChain(char chainID)
        {
            return chains[chainID];
        }

        public void AddChain(char chainID, Chain newChain)
        {
            if (!chains.ContainsKey(chainID))
            {
                chains.Add(chainID, newChain);
            }
            else
            {
                throw new InvalidOperationException("Chain " + chainID + " already present.");
            }
        }

        public void AddResidue(Residue residue)
        {
            foreach (Atom atom in residue.GetAtoms())
            {
                AddAtom(atom);
            }
        }

        public void AddAtom(Atom atom)
        {
            if (!chains.ContainsKey(atom.ChainID))
            {
                chains.Add(atom.ChainID, new Chain());
            }
            chains[atom.ChainID].AddAtom(atom);
        }
    }
}
