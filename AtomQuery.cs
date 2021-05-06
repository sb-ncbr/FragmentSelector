using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FragmentSelector
{
    class AtomQuery
    {
        public string Chain { get; private set; }  // chain ID
        public int? Resi { get; private set; }  // residue number
        public string Resn { get; private set; }  // residue name, e.g. ALA
        public string Name { get; private set; }  // atom name, e.g. CA
        public int? Id { get; private set; }  // atom ID

        public override string ToString(){
            List<string> parts = new List<string>();
            if (this.Chain != null) parts.Add($"chain={Chain}");
            if (this.Resi != null) parts.Add($"resi={Resi}");
            if (this.Resn != null) parts.Add($"resi={Resn}");
            if (this.Name != null) parts.Add($"name={Name}");
            if (this.Id != null) parts.Add($"id={Id}");  
            return string.Join(" & ", parts);
        }

        public AtomQuery(string query)
        {
            
            int cai;
            if (Int32.TryParse(query, out cai)) {
                Id = cai;
                return;
            }

            string[] parts = query.Split('&');
            HashSet<string> seenParams = new HashSet<string>();
            foreach(string part in parts){
                string[] paramValue = part.Split('=');
                if (paramValue.Length != 2) {
                    throw new FormatException($"Invalid atom query '{query}'.");
                }
                string param = paramValue[0].Trim().ToLower();
                string value = paramValue[1].Trim();
                if (seenParams.Contains(param)) throw new FormatException($"Repeating parameter '{param}' in atom query.");
                seenParams.Add(param);
                int num;
                switch (param) {
                    case "chain":
                        Chain = value;
                        break;
                    case "resn":
                        Resn = value;
                        break;
                    case "name":
                        Name = value;
                        break;
                    case "resi":
                        if (Int32.TryParse(value, out num)) Resi = num;
                        else throw new FormatException($"Parameter '{param}' has invalid value '{value}' in atom query (must be integer).");
                        break;
                    case "id":
                        if (Int32.TryParse(value, out num)) Id = num;
                        else throw new FormatException($"Parameter '{param}' has invalid value '{value}' in atom query (must be integer).");
                        break;
                    default:
                        throw new FormatException($"Unknown parameter '{param}' in atom query.");
                }
            }
        }

        public Atom[] FindInProtein(Protein protein){
            Atom[] atoms = protein.GetAtoms().ToArray();
            if (this.Id != null){
                atoms = atoms.Where(atom => atom.Serial == this.Id).ToArray();
            }
            if (this.Chain != null){
                atoms = atoms.Where(atom => atom.ChainID.ToString() == this.Chain).ToArray();
            }
            if (this.Resn != null){
                atoms = atoms.Where(atom => atom.ResName == this.Resn).ToArray();
            }
            if (this.Resi != null){
                atoms = atoms.Where(atom => atom.ResSeq == this.Resi).ToArray();
            }
            if (this.Name != null){
                atoms = atoms.Where(atom => atom.Name.Trim() == this.Name).ToArray();
            }
            return atoms;
        }
    }
}
