using UnityEngine;

[System.Serializable]
public class PersistentVariables
{
    public string defaultFileLocation;
    public string MewgenicsDirectory;
    public PersistentVariables()
    {
        defaultFileLocation = "C:\\Users";
        MewgenicsDirectory = "";
    }
    public void Copy(PersistentVariables copy)
    {
        defaultFileLocation = copy.defaultFileLocation;
        MewgenicsDirectory = copy.MewgenicsDirectory;
    }

}
