# FragmentSelector

The goal of FragmentSelector is selecting a fragment from
a protein. The requirements for the resulting fragment are:

- The fragment should contain the atom *A* and its closest surroundings, i.e. atoms within the radius *r*.
- The number of bonds broken by the selection should be minimal.
- The number of atoms in the fragment must not be higher than *N* but should be as close to *N* as possible (this is less important than the previous requirements).

An input of the program is a PDB file containing a protein and the parameters *A*,
*r*, and *N*. Other parameters can be set by options. The output is a file containing atoms of the selected fragment in PDB format. (The output file will only contain ATOM and HETATMS records.)

## Dependencies

- .NET Core 3.1 (`dotnet`)
- PyMOL (`pymol`) - only if you want to use `-r` option

## Files

All files needed for execution are found in directory `release/`.

## Execution

Print version and help message:

    dotnet release/FragmentSelector.dll

Cut fragment with at most 500 atoms around atom 3810 in `data/1tqn.pdb` with required radius 10 Angstroms. Save result in `data/1tqn.fragment.pdb`:

    dotnet release/FragmentSelector.dll data/1tqn.pdb 3810 8 500 -o data/1tqn.fragment.pdb

The same, but use algorithm mode 1, print partial result from each step, and create a PyMOL session with visualization of the results:

    dotnet release/FragmentSelector.dll data/1tqn.pdb 3810 8 500 -o data/1tqn.fragment.pdb -m 1 -s -r

## Remarks

Using the `-r` option (create PyMOL session) requires that PyMOL be installed on the system, and that `release/config.txt` contain the path to the PyMOL executable.

Multiple options must not be grouped together (e.g. `-s -r` cannot be shortened to `-sr`).

## More information

Details can be found in this work:

- MIDLIK, Adam. Selection of protein fragments using minimal bond breaking [online]. Brno, 2014 [cit. 2021-03-18]. Available from: <https://is.muni.cz/th/pqum6/>. Bachelor's thesis. Masaryk University, Faculty of Informatics. Thesis supervisor Radka Svobodová.
