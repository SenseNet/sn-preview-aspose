************************************************************************************
                                  sensenet platform
                               Aspose Preview Provider
************************************************************************************

To finalize the installation and get started with sensenet Aspose Preview Provider, please follow these steps:

1. Build your solution, make sure that there are no build errors.

2. Install sensenet Aspose Preview Provider 
    a) in a .Net Core environment
        Using the Installer API in a separate console project.

        var installer = new SenseNet.Packaging.Installer(builder);
                .InstallSenseNet()
                .InstallPreview()
                .InstallPreviewAspose();

    b) Legacy mode: using SnAdmin in a .Net Framework environment

   - open a command line and go to the \Admin\bin folder
   - execute the following command with the SnAdmin tool

   .\snadmin install-preview-aspose
      

For more information and support, please visit http://sensenet.com