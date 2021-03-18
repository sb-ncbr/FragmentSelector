using System;
using System.IO;

namespace FragmentSelector {
    public class Config {
        public String PymolExecutable { get; private set; }
        public String PymolScript { get; private set; }

        public Config(String configFileName) {
            
            using (var reader = new StreamReader(configFileName)){
                while (true) {
                    string line = reader.ReadLine();
                    if (line == null){
                        break;
                    }
                    line = line.Trim();
                    if (line=="" || line.StartsWith('#')){
                        continue;
                    }
                    var parts = line.Split(':', 2);
                    if (parts.Length != 2) {
                        throw new FormatException($"Invalid line '{line}' in configuration file '{configFileName}'");
                    }
                    string key = parts[0].Trim();
                    string value = parts[1].Trim();
                    switch(key){
                        case "PymolExecutable":
                            PymolExecutable = value;
                            break;
                        case "PymolScript":
                            PymolScript = value;
                            break;
                        default:
                            throw new FormatException($"Unknown key '{key}' in configuration file '{configFileName}'");
                    }
                }
            }
            if (PymolExecutable == null){
                throw new FormatException($"Missing key 'PymolExecutable' in configuration file '{configFileName}'");
            }
            if (PymolScript == null){
                throw new FormatException($"Missing key 'PymolScript' in configuration file '{configFileName}'");
            }
        }

    }
}

