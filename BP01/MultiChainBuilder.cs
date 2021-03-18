using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BP01
{
    class MultiChainBuilder
    {
        private Dictionary<char, ChainBuilder> cbs;

        public MultiChainBuilder()
        {
            cbs = new Dictionary<char, ChainBuilder>();
        }

        public MultiChainBuilder(ICollection<Atom> atoms)
            : this()
        {
            foreach (Atom atom in atoms) { AddAtom(atom); }
        }

        public MultiChainBuilder(ICollection<Residue> residues)
            : this()
        {
            foreach (Residue residue in residues) { AddResidue(residue); }
        }

        private void AddChainBuilder(char chainID, ChainBuilder cb)
        {
            if (cbs.ContainsKey(chainID)) throw new InvalidOperationException();
            cbs.Add(chainID, cb);
        }

        public void AddResidue(Residue newResidue)
        {
            if (!cbs.ContainsKey(newResidue.ChainID))
            {
                AddChainBuilder(newResidue.ChainID, new ChainBuilder());
            }
            cbs[newResidue.ChainID].AddResidue(newResidue);
        }

        public void AddAtom(Atom atom)
        {
            Program.logger.Debug("MultiChainBuilder.AddAtom("+atom+") to chainBuilder" + atom.ChainID);
            if (!cbs.ContainsKey(atom.ChainID))
            {
                AddChainBuilder(atom.ChainID, new ChainBuilder());
            }
            cbs[atom.ChainID].AddAtom(atom);
        }

        public void Clear()
        {
            cbs = new Dictionary<char, ChainBuilder>();
        }

        public SortedSet<Residue> GetResidues()
        {
            SortedSet<Residue> result = new SortedSet<Residue>();
            foreach (ChainBuilder cb in cbs.Values)
            {
                result.UnionWith(cb.GetResidues());
            }
            return result;
        }

        public SortedSet<Atom> GetAtoms()
        {
            SortedSet<Atom> result = new SortedSet<Atom>();
            foreach (ChainBuilder cb in cbs.Values)
            {
                result.UnionWith(cb.GetAtoms());
            }
            return result;
        }

        public SortedSet<Chain> GetChains()
        {
            Program.logger.OpenBlockD("MultiChainBuilder.GetChains()");
            SortedSet<Chain> result = new SortedSet<Chain>();
            foreach (char chainID in cbs.Keys)
            {
                Program.logger.Debug("Processing ChainBuilder " + chainID + "...");
                result.UnionWith(cbs[chainID].GetChains());
            }
            Program.logger.CloseBlockD("MultiChainBuilder.GetChains()");
            return result;
        }
    }
}
