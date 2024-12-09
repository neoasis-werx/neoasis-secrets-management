# PowerShell SecretManagement module

PowerShell SecretManagement module provides a convenient way for a user to store and retrieve secrets.
The secrets are stored in SecretManagement extension vaults.
An extension vault is a PowerShell module that has been registered to SecretManagement, and exports five module functions required by SecretManagement.
An extension vault can store secrets locally or remotely.
Extension vaults are registered to the current logged in user context, and will be available only to that user (unless also registered to other users).  

In addition to implementing the five required SecretManagement functions, extension vaults are responsible for any authentication, including prompting the user for passphrases, and providing error and informational messages specific to the vault implementation.
Error and informational messages common to all extension vaults are provided by SecretManagement.  

PowerShell SecretManagement module is essentially an orchestrator for extension vaults which perform the actual secret storage and encryption.
The extension vault may implement its own store, or wrap an existing secure store solution, such as Azure KeyVault, KeePass, Keyring, etc.  

PowerShell SecretManagement supports the following secret data types:

- byte[]
- string
- SecureString
- PSCredential
- Hashtable  

All extension vault implementations must also support these data types.  

PowerShell SecretManagement module provides cmdlets for for accessing and manipulating secrets, and also cmdlets for registering and manipulating vault extensions.  

## Installation

You can install the `Microsoft.PowerShell.SecretManagement` module from the [PowerShell Gallery](https://www.powershellgallery.com/packages/Microsoft.PowerShell.SecretManagement).  

```powershell
Install-Module -Name Microsoft.PowerShell.SecretManagement -Repository PSGallery
```

## Secret metadata

Extension vaults can optionally support storing and retrieving additional secret metadata, that is data associated with the secret.  
Secret metadata is not security sensitive and does not need to be stored securely in the extension vault.  

Secret metadata can be included by using the `-Metadata` parameter: `Set-Secret -Metadata @{ Name=Value }`.
The `-Metadata` parameter takes a `Hashtable` type argument consisting of name/value pairs.  
Extension vaults should at minimum support the following value types:  

- string
- int
- DateTime

The secret metadata is included in the `SecretInformation` type object returned by `Get-SecretInfo`, in a `Metadata` property.  
The `Metadata` property is a `ReadonlyDictionary<string, object>` type.  

## Vault extension registration cmdlets

### Register-SecretVault

Registers a single extension vault to the current user

### Get-SecretVault

Retrieves information about one or more registered extension vaults

### Unregister-SecretVault

Unregisters a single extension vault

### Set-SecretVaultDefault

Sets one registered extension vault as the default vault

### Test-SecretVault

Runs the Test-SecretVault function provided by the extension vault

### Unlock-SecretVault

Unlocks the extension vault through a passed in SecureString password

## Accessing secrets cmdlets

### Set-Secret

Adds a secret to a specific extension vault, or to the default vault if no vault is specified

### Set-SecretInfo

Adds or replaces additional secret metadata to an existing secret in the vault.
Metadata is not stored securely.

### Get-Secret

Retrieves a secret from a specific extension vault, or first found over all vaults

### Get-SecretInfo

Retrieves information about one or more secrets, but not the secret itself

### Remove-Secret

Removes a secret from a specific vault

## Vault extension registration

Vault extensions are registered to the current user context.
They are registered as PowerShell modules that expose required functions used by SecretManagement to manipulate secrets.
The required functions are provided as PowerShell cmdlets, and can be implemented as script or binary cmdlets.  

Since each extension vault module exports the same five cmdlets, the module must conform to a directory structure that hides cmdlets from the user and PowerShell command discovery.
Therefore the extension vault module itself does not export the five required cmdlets directly, but are instead exported from a nested module that resides within the extension vault module directory.
This nested module must have a name that is the parent module name with '.Extension' appended to it.  

It is recommended that the parent module manifest file (.psd1) include the 'SecretManagement' tag in its PrivateData section.
This allows [PowerShellGallery](https://www.powershellgallery.com) to associate it with the SecretManagement module.  

### Example: Script module vault extension

This is a minimal vault extension example to demonstrate the directory structure and functional requirements of an extension vault module.
The extension vault module name is 'TestVault'.  

#### Module directory structure

./TestVault  
./TestVault/TestVault.psd1  
./TestVault/TestStoreImplementation.dll  
./TestVault/TestVault.Extension  
./TestVault/TestVault.Extension/TestVault.Extension.psd1  
./TestVault/TestVault.Extension/TestVault.Extension.psm1  

#### TestVault.psd1 file

```powershell
@{
    ModuleVersion = '1.0'
    RootModule = '.\TestStoreImplementation.dll'
    NestedModules = @('.\TestVault.Extension')
    CmdletsToExport = @('Set-TestStoreConfiguration','Get-TestStoreConfiguration')
    PrivateData = @{
        PSData = @{
            Tags = @('SecretManagement')
        }
    }
}
```

The TestVault extension module has a binary component (TestStoreImplementation.dll) which implements the vault.  It publicly exports two cmdlets that are used to configure the store.
It also declares the required nested module (TestVault.Extension) that exports the five functions required by SecretManagement registration.  

Note that the nested module conforms to the required naming format:  
'[ModuleName].Extension'  

Note that only the 'NestedModules' entry is required because it loads 'TestVault.Extension' into the module scope, and allows SecretManagement access to the required five functions.
The 'RootModule' and 'CmdletsToExport' entries are only for configuring the TestStore in this specific case, and are not needed in general.  

#### TestVault.Extension.psd1 file

```powershell
@{
    ModuleVersion = '1.0'
    RootModule = '.\TestVault.Extension.psm1'
    RequiredAssemblies = '..\TestStoreImplementation.dll'
    FunctionsToExport = @('Set-Secret','Get-Secret','Remove-Secret','Get-SecretInfo','Test-SecretVault')
}
```

This nested module implements and exports the five functions required by SecretManagement.
It also specifies the TestStoreImplementation.dll binary as a 'RequiredAssemblies' because the five exported functions depend on it.  

#### TestVault.Extension.psm1 file

```powershell
function Get-Secret
{
    [CmdletBinding()]
    param (
        [string] $Name,
        [string] $VaultName,
        [hashtable] $AdditionalParameters
    )

    return [TestStore]::GetItem($Name, $AdditionalParameters)
}

function Get-SecretInfo
{
    [CmdletBinding()]
    param (
        [string] $Filter,
        [string] $VaultName,
        [hashtable] $AdditionalParameters
    )

    return @(,[Microsoft.PowerShell.SecretManagement.SecretInformation]::new(
        "Name",        # Name of secret
        "String",      # Secret data type [Microsoft.PowerShell.SecretManagement.SecretType]
        $VaultName,    # Name of vault
        $Metadata))    # Optional Metadata parameter
}

function Set-Secret
{
    [CmdletBinding()]
    param (
        [string] $Name,
        [object] $Secret,
        [string] $VaultName,
        [hashtable] $AdditionalParameters
    )

    [TestStore]::SetItem($Name, $Secret)
}

# Optional function
function Set-SecretInfo
{
    [CmdletBinding()]
    param (
        [string] $Name,
        [hashtable] $Metadata,
        [string] $VaultName,
        [hashtable] $AdditionalParameters
    )

    [TestStore]::SetItemMetadata($Name, $Metadata)
}

function Remove-Secret
{
    [CmdletBinding()]
    param (
        [string] $Name,
        [string] $VaultName,
        [hashtable] $AdditionalParameters
    )

    [TestStore]::RemoveItem($Name)
}

function Test-SecretVault
{
    [CmdletBinding()]
    param (
        [string] $VaultName,
        [hashtable] $AdditionalParameters
    )

    return [TestStore]::TestVault()
}

# Optional function
function Unregister-SecretVault
{
    [CmdletBinding()]
    param (
        [string] $VaultName,
        [hashtable] $AdditionalParameters
    )

    [TestStore]::RunUnregisterCleanup()
}

# Optional function
function Unlock-SecretVault
{
    [CmdletBinding()]
    param (
        [SecureString] $Password,
        [string] $VaultName,
        [hashtable] $AdditionalParameters
    )

    [TestStore]::UnlockVault($Password)
}
```

This module script implements the five functions, as cmdlets, required by SecretManagement, plus some optional functions.
It also implements an optional `Unregister-SecretVault` function that is called during vault extension un-registration.
It also implements an optional `Set-SecretInfo` function that sets secret metadata to a specific secret in the vault.
It also implements an optional `Unlock-SecretVault` function that unlocks the vault for the current session based on a provided password.  

The `Set-Secret`, `Set-SecretInfo`, `Remove-Secret`, `Unregister-SecretVault` functions do not write any data to the pipeline, i.e., they do not return any data.  

The `Get-Secret` cmdlet writes the retrieved secret value to the output pipeline on return, or null if no secret was found.
It should write an error only if an abnormal condition occurs.  

The `Get-SecretInfo` cmdlet writes an array of `Microsoft.PowerShell.SecretManagement.SecretInformation` type objects to the output pipeline or an empty array if no matches were found.  

The `Test-SecretVault` cmdlet should write all errors that occur during the test.
But only a single true/false boolean should be written the the output pipeline indicating success.  

The `Unregister-SecretVault` cmdlet is optional and will be called on the extension vault if available.
It is called before the extension vault is unregistered to allow it to perform any needed clean up work.  

The `Unlock-SecretVault` cmdlet is optional and will be called on the extension vault if available.
It's purpose is to unlock an extension vault for use without having to prompt the user, and is useful for unattended scripts where user interaction is not possible.  

In general, these cmdlets should write to the error stream only for abnormal conditions that prevent successful completion.
And write to the output stream only the data as indicated above, and expected by SecretManagement.  

In addition, these cmdlets should perform proper authentication and provide errors, and instructions to authenticate, as appropriate.
Or prompt the user if needed, for example if a passphrase is required.  

A vault extension doesn't need to provide full implementation of all required functions.
For example, a vault extension does not need to provide a way to add or remove a secret through the SecretManagement cmdlets, and can just provide retrieval services.
If a vault extension doesn't support some functionality, then it should write an appropriate error with a meaningful message.  

Be careful with module implementation with scripts, because any data returned by function or method calls are automatically written to the output pipeline (if not assigned to a variable).
SecretManagement expects only specific data to appear in the output pipeline, and if other data is inadvertently written, that will cause SecretManagement to not function properly.  

### Registering the vault

Once the TestVault module is created, it is registered as follows:

```powershell
Register-SecretVault -Name LocalStore -ModuleName ./TestVault -VaultParameters @{ None="ReallyNeeded" } -DefaultVault

Get-SecretVault

VaultName  ModuleName  IsDefaultVault
---------  ----------  --------------
LocalStore TestVault   True

```

## Extension vault registry file location

SecretManagement is designed to be installed and run within a user account on both Windows and non-Windows platforms.
The extension vault registry file is located in a user account protected directory.  

For Windows platforms the location is: `%LOCALAPPDATA%\Microsoft\PowerShell\secretmanagement`

For non-Windows platforms the location: `$HOME/.secretmanagement`

## Windows Managed Accounts

SecretManagement does not currently work for Windows managed accounts.  

SecretManagement depends on both `%LOCALAPPDATA%` folders to store registry information, and Data Protection APIs for safely handling secrets with the .Net `SecureString` type.  
However, Windows managed accounts do not have profiles or `%LOCALAPPDATA%` folders, and Windows Data Protection APIs do not work for managed accounts.  
Consequently, SecretManagement will not run under managed accounts.

## Code of Conduct

Please see our [Code of Conduct](.github/CODE_OF_CONDUCT.md) before participating in this project.

## Security Policy

For any security issues, please see our [Security Policy](.github/SECURITY.md).


---
# Get-Secret

## SYNOPSIS
Finds and returns a secret by name from registered vaults.

## SYNTAX

### NameParameterSet (Default)
```
Get-Secret [-Name] <String> [[-Vault] <String>] [-AsPlainText] [<CommonParameters>]
```

### InfoParameterSet
```
Get-Secret [-InputObject] <SecretInformation> [-AsPlainText] [<CommonParameters>]
```

## DESCRIPTION
This cmdlet finds and returns the first secret that matches the provided name.
If a vault name is specified, then only that vault will be searched.
Otherwise, all vaults are searched and the first found result is returned.
If a 'Default' vault is specified, then that vault is searched before any other registered vault.
Secrets that are string or SecureString types are returned as SecureString objects by default.
Unless the '-AsPlainText' parameter switch is used, in which case the secret is returned as a String type in plain text.

## EXAMPLES

### Example 1
```
PS C:\> Get-Secret -Name Secret1 -Vault CredMan
System.Security.SecureString

PS C:\> Get-Secret -Name Secret1 -Vault CredMan -AsPlainText
PlainTextSecretString
```

This example searches for a secret with the name 'Secret1', which is a String type secret.
The first time returns the secret as a SecureString object.
The second time uses the '-AsPlainText' and so the secret string is returned as a string object, and is displayed in plain text.

### Example 2
```
PS C:\> Get-SecretInfo -Name Secret2 -Vault SecretStore | Get-Secret -AsPlainText
```

This example retrieves secret information for the secret named 'Secret2' and then pipe the result to 'Get-Secret'.
The secret is then looked up in the SecretStore vault and returned as plain text.

## PARAMETERS

### -AsPlainText
Switch parameter that when used returns either a string or SecureString secret type as a String type (in plain text).
If the secret being retrieved is not of string or SecureString type, this switch parameter has no effect.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -InputObject
SecretInformation object that describes a vault secret.

```yaml
Type: SecretInformation
Parameter Sets: InfoParameterSet
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Name
Name of the secret to be retrieved.
Wild card characters are not allowed.

```yaml
Type: String
Parameter Sets: NameParameterSet
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Vault
Optional name of the registered vault to retrieve the secret from.
If no vault name is specified, then all registered vaults are searched.

```yaml
Type: String
Parameter Sets: NameParameterSet
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String
### Microsoft.PowerShell.SecretManagement.SecretInformation
## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS

---

# Set-Secret

## SYNOPSIS
Adds a secret to a SecretManagement registered vault.

## SYNTAX

### SecureStringParameterSet (Default)
```
Set-Secret [-Name] <String> -SecureStringSecret <SecureString> [[-Vault] <String>] [[-Metadata] <Hashtable>]
 [-NoClobber] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### ObjectParameterSet
```
Set-Secret [-Name] <String> -Secret <Object> [[-Vault] <String>] [[-Metadata] <Hashtable>] [-NoClobber]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

### SecretInfoParameterSet
```
Set-Secret -SecretInfo <SecretInformation> [-Vault] <String> [-NoClobber] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## DESCRIPTION
This cmdlet adds a secret value by name to SecretManagement.
If no vault name is specified, then the secret will be added to the default vault.
If an existing secret by the same name exists, it will be overwritten with the new value unless the 'NoClobber' parameter switch is used.
Additional data can be included with the secret through the '-Metadata' parameter, if supported by the extension vault.
If the extension vault does not support metadata then an error will be generated and the operation will fail.
Metadata is not required to be stored securely, and should not contain sensitive information.
The secret value must be one of five supported types:

- byte[]
- String
- SecureString
- PSCredential
- Hashtable

The default parameter set takes a SecureString object.
So if the command is run without specifying the secret value, the user will be safely prompted to enter a SecureString which cannot be seen on the console.

## EXAMPLES

### Example 1
```powershell
PS C:\> Set-Secret -Name Secret1 -Secret "SecretValue"
PS C:\> Get-Secret -Name Secret1
System.Security.SecureString
```

This example adds a secret named 'Secret1' with a plain text value of 'SecretValue'.
Since no vault name was specified, the secret is added to the current default vault.
Next, the 'Get-Secret' command is run to verify the added secret.

### Example 2
```powershell
PS C:\> Set-Secret -Name Secret2 -Vault LocalStore

cmdlet Set-Secret at command pipeline position 1
Supply values for the following parameters:
SecureStringSecret: ***********

PS C:\> Get-Secret -Name Secret2
System.Security.SecureString
```

This example adds a secret named 'Secret2' to the LocalStore vault.
Since no secret value was provided, the user is prompted for a SecureString value.
The console hides the string value as it is typed.
Next, the 'Get-Secret' command is run to verify the secret was added.

### Example 3
```powershell
PS C:\> Set-Secret -Name TargetSecret -Secret $targetToken -Vault LocalStore -Metadata @{ Expiration = ([datetime]::new(2022, 5, 1)) }
PS C:\> Get-SecretInfo -Name TargetSecret | Select-Object Name,Metadata

Name         Metadata
----         --------
TargetSecret {[Expiration, 5/1/2022 12:00:00 AM]}
```

This example adds a secret named 'TargetSecret' to the LocalStore vault, along with extra metadata indicating the secret expiration date.
The metadata is retrieved using the 'Get-SecretInfo' cmdlet.

### Example 4
```powershell
PS C:\> Set-Secret -Name PublishSecret -Secret $targetToken -Vault LocalStore2 -Metadata @{ Expiration = ([datetime]::new(2022, 5, 1)) }
Set-Secret: Cannot store secret PublishSecret. Vault LocalStore2 does not support secret metadata.
```

This example adds a secret named 'PublishSecret' to the LocalStore2 vault, along with extra metadata.
However, vault LocalStore2 does not support secret metadata and the operation fails with error.

## PARAMETERS

### -Metadata
Hashtable containing Name/Value pair that are stored in the vault.
The specified extension vault may not support secret metadata, in which case the operation will fail.
The metadata Name/Value value type must be one of the following:
- string
- int
- DateTime

```yaml
Type: Hashtable
Parameter Sets: SecureStringParameterSet, ObjectParameterSet
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
Name of secret for which the metadata is added

```yaml
Type: String
Parameter Sets: SecureStringParameterSet, ObjectParameterSet
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NoClobber
When true, this command will not overwrite an existing secret and emit an error instead.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -Secret
A secret value to be added.
The object type must be one of the supported types.

```yaml
Type: Object
Parameter Sets: ObjectParameterSet
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -SecretInfo
A SecretInformation object describing a stored secret returned by 'Get-SecretInfo'.
This allows moving secrets from one extension vault to another.

```yaml
Type: SecretInformation
Parameter Sets: SecretInfoParameterSet
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -SecureStringSecret
A secret SecretString object to be added.

```yaml
Type: SecureString
Parameter Sets: SecureStringParameterSet
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Vault
Optional name of vault to which the secret is added.
If omitted, the secret will be added to the default vault.

```yaml
Type: String
Parameter Sets: SecureStringParameterSet, ObjectParameterSet
Aliases:

Required: False
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

```yaml
Type: String
Parameter Sets: SecretInfoParameterSet
Aliases:

Required: True
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Confirm
Prompts you for confirmation before running the cmdlet.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf
Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.Collections.Hashtable
## OUTPUTS

## NOTES

## RELATED LINKS

---

# Remove-Secret

## SYNOPSIS
Removes a secret from a specified registered extension vault.

## SYNTAX

### NameParameterSet
```
Remove-Secret [-Name] <String> [-Vault] <String> [-WhatIf] [-Confirm] [<CommonParameters>]
```

### InfoParameterSet
```
Remove-Secret [-InputObject] <SecretInformation> [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
This cmdlet will remove a secret by name from a registered extension vault.
Both the secret name and extension vault name must be provided.

## EXAMPLES

### Example 1
```powershell
PS C:\> Remove-Secret -Name secretTest -Vault CredMan
PS C:\> Get-Secret -Name secretTest -Vault CredMan
Get-Secret: The secret secretTest was not found.
```

This example runs the command to remove the secret 'secretTest' from the CredMan vault.
The 'Get-Secret' command is next run to verify the secret no longer exists in the vault.

### Example 2
```
PS C:\> Get-SecretInfo -Name Secret2 -Vault CredMan | Remove-Secret
PS C:\> Get-Secret -Name Secret2 -Vault CredMan
Get-Secret: The secret Secret2 was not found.
```

This example first obtains secret information for the 'Secret2' secret and pipes the results to this command.
Remove-Secret then removes the secret from the vault using the piped in secret information.

## PARAMETERS

### -InputObject
SecretInformation object that describes a vault secret.

```yaml
Type: SecretInformation
Parameter Sets: InfoParameterSet
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Name
Name of the secret to remove.

```yaml
Type: String
Parameter Sets: NameParameterSet
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Vault
Name of the vault from which the secret is to be removed.

```yaml
Type: String
Parameter Sets: NameParameterSet
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Confirm
Prompts you for confirmation before running the cmdlet.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf
Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String
### Microsoft.PowerShell.SecretManagement.SecretInformation
## OUTPUTS

## NOTES

## RELATED LINKS

---

# Get-SecretInfo

## SYNOPSIS
Finds and returns secret metadata information of one or more secrets.

## SYNTAX

```
Get-SecretInfo [[-Name] <String>] [[-Vault] <String>] [<CommonParameters>]
```

## DESCRIPTION
This cmdlet finds and returns secret metadata for secrets with names that match the provided 'Name'.
The 'Name' parameter argument can include wildcards for the search.
If no 'Name' parameter argument is provided then metadata for all secrets is returned.
The search is performed over all registered vaults, unless a specific vault name is specified.
Secret metadata consists of the secret name, secret type, vault name, and optional additional user data if supported by the extension vault.

## EXAMPLES

### Example 1
```powershell
PS C:\> Get-SecretInfo -Name *

Name                    Type VaultName
----                    ---- ---------
Secret1               String LocalStore
Secret2            ByteArray LocalStore
Secret3         SecureString LocalStore
Secret4         PSCredential LocalStore
Secret5            Hashtable LocalStore
Secret6            ByteArray CredMan
```

This example runs the command with the 'Name' parameter argument being a single wildcard character.
So all metadata for all stored secrets is returned.
There are two registered vaults, LocalStore and CredMan.
There are six secrets metadata information returned over the two vaults.

### Example 2
```powershell
PS C:\> Get-SecretInfo -Name APIKey | Select-Object Name,VaultName,Metadata

Name     VaultName    Metadata
----     ---------    --------
APIKey   SecretStore  {[Target, PSGallery]}
```

This example runs the command for a single secret name and displays the secret name, the vault name, and any metadata associated with the secret.

## PARAMETERS

### -Name
This parameter takes a String argument, including wildcard characters.
It is used to filter the search results that match on secret names the provided name pattern.
If no 'Name' parameter argument is provided, then all stored secret metadata is returned.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -Vault
Optional parameter which takes a String argument that specifies a single vault to search.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### Microsoft.PowerShell.SecretManagement.SecretInformation
## NOTES

## RELATED LINKS


---
# Set-SecretInfo

## SYNOPSIS
Adds or replaces additional secret metadata to a secret currently stored in a vault.

## SYNTAX

### NameParameterSet (Default)
```
Set-SecretInfo [-Name] <String> [-Metadata] <Hashtable> [[-Vault] <String>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

### InfoParameterSet
```
Set-SecretInfo [-Metadata] <Hashtable> -InputObject <SecretInformation> [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## DESCRIPTION
This cmdlet adds additional secret metadata to an existing secret.
Metadata support is an optional feature for an extension vault.
An error will be thrown if a vault does not support secret metadata.
Metadata is a Hashtable object containing Name/Value pairs.
The value type is restricted to the following:

- string
- int
- DateTime

Metadata is not stored securely in a vault.
Metadata should not contain sensitive information.

## EXAMPLES

### Example 1
```powershell
PS C:\> Set-SecretInfo -Name Secret1 -Vault Vault1 -Metadata @{ Expiration = ([datetime]::new(2022, 5, 1)) }
PS C:\> Get-SecretInfo -Name Secret1 -Vault Vault1 | Select-Object Name,Metadata

Name         Metadata
----         --------
Secret1 {[Expiration, 5/1/2022 12:00:00 AM]}
```

This example adds metadata to the 'Secret1' secret stored in 'Vault1' vault.
The metadata is then retrieved for 'Secret1' using the 'Get-SecretInfo' command.

### Example 2
```powershell
PS C:\> Set-SecretInfo -Name Secret2 -Vault Vault2 -Metadata @{ Expiration = ([datetime]::new(2022, 5, 1)) }
Set-SecretInfo: Cannot set secret metadata Secret2. Vault Vault2 does not support secret metadata.
```

This example adds metadata to the 'Secret2' secret stored in 'Vault2' vault.
However, Vault2 does not support metadata and an error is generated.

### Example 3
```powershell
PS C:\> Get-SecretInfo -Name Secret3 | Set-SecretInfo -Metadata @{ Created = (Get-Date) }
```

This example pipes a SecretInformation object to the 'Set-SecretInfo' command and adds metadata to the associated secret.

## PARAMETERS

### -InputObject
This parameter takes a SecretInformation object that defines the secret to be updated.

```yaml
Type: SecretInformation
Parameter Sets: InfoParameterSet
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Metadata
Hashtable containing Name/Value pair that are stored in the vault.
The specified extension vault may not support secret metadata, in which case the operation will fail.
The metadata Name/Value value type must be one of the following:
- string
- int
- DateTime

```yaml
Type: Hashtable
Parameter Sets: NameParameterSet
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

```yaml
Type: Hashtable
Parameter Sets: InfoParameterSet
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Name
Name of secret for which the metadata is added.

```yaml
Type: String
Parameter Sets: NameParameterSet
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Vault
Optional name of vault to which the secret is added.
If omitted, the secret will be added to the default vault.

```yaml
Type: String
Parameter Sets: NameParameterSet
Aliases:

Required: False
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Confirm
Prompts you for confirmation before running the cmdlet.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf
Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.Collections.Hashtable
## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS

---

# Get-SecretVault

## SYNOPSIS
Finds and returns registered vault information.

## SYNTAX

```
Get-SecretVault [[-Name] <String[]>] [<CommonParameters>]
```

## DESCRIPTION
This cmdlet finds and returns information of registered vaults.
It takes an array of vault name strings, which can contain wildcard characters.
If no 'Name' parameter is specified, all registered vault information is returned.
The registered vault information includes the vault name, vault implementing module name, and optional default parameters.

## EXAMPLES

### Example 1
```
PS C:\> Get-SecretVault

VaultName  ModuleName                        IsDefaultVault
---------  ----------                        --------------
CredMan    Microsoft.PowerShell.CredManStore False
LocalStore Microsoft.PowerShell.SecretStore  True
```

This example runs the command without any parameter arguments, and so returns information on all registered vaults.
The 'LocalStore' vault is shown to be set as the default vault.

## PARAMETERS

### -Name
This parameter takes a String argument, including wildcard characters.
It is used to filter the search results on vault names that match the provided name pattern.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### Microsoft.PowerShell.SecretManagement.SecretVaultInfo
## NOTES

## RELATED LINKS

---
# Register-SecretVault

## SYNOPSIS
Registers a SecretManagement extension vault module for the current user.

## SYNTAX

```
Register-SecretVault [-ModuleName] <String> [[-Name] <String>] [-VaultParameters <Hashtable>] [-DefaultVault]
 [-AllowClobber] [-PassThru] [-Description <String>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
This cmdlet adds a provided SecretManagement extension vault module to the current user vault registry.
An extension vault module is a PowerShell module that conforms to the required extension vault format.
This cmdlet will first verify that the provided module meets conformance requirements, and then add it to the extension vault registry.
Extension vaults are registered to the current user and do not affect other user vault registrations.

## EXAMPLES

### Example 1
```powershell
PS C:\> Register-SecretVault -Name LocalStore -ModuleName Microsoft.PowerShell.SecretStore  -DefaultVault
PS C:\> Get-SecretVault

VaultName  ModuleName                        IsDefaultVault
---------  ----------                        --------------
CredMan    Microsoft.PowerShell.CredManStore False
LocalStore Microsoft.PowerShell.SecretStore  True
```

This example registers the Microsoft.PowerShell.SecretStore extension vault module for the current user.
The 'Microsoft.PowerShell.SecretStore' is installed in a known PowerShell module path, so just the module name is needed.
It uses the 'DefaultVault' parameter switch to make it the default module for the user.
The 'Get-SecretVault' command is run next to list all registered vaults for the user, and verifies the vault was registered and set as the default vault.

## PARAMETERS

### -AllowClobber
When used this parameter will overwrite an existing registered extension vault with the same name.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -DefaultVault
This parameter switch makes the new extension vault the default vault for the current user.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -Description
This parameter takes a description string that is included in the vault registry information.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ModuleName
Name of the PowerShell module that implements the extension vault.
It can be a simple name, in which case PowerShell will search for it in its known module paths.
Alternatively, a pathname can be provided and PowerShell will look in the specific path for the module.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
Name of the extension vault to be registered.
If no name is provide, the module name will be used.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PassThru
When used this parameter will return the SecretVaultInfo object for the successfully registered extension vault.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -VaultParameters
This takes a hashtable object that contains optional parameter name-value pairs needed by the extension vault.
These optional parameters are provided to the extension vault when invoked.

```yaml
Type: Hashtable
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Confirm
Prompts you for confirmation before running the cmdlet.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf
Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

## NOTES

## RELATED LINKS

---

# Unregister-SecretVault

## SYNOPSIS
Un-registers an extension vault from SecretManagement for the current user.

## SYNTAX

### NameParameterSet (Default)
```
Unregister-SecretVault [-Name] <String[]> [-WhatIf] [-Confirm] [<CommonParameters>]
```

### SecretVaultParameterSet
```
Unregister-SecretVault [-SecretVault] <SecretVaultInfo> [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
This cmdlet un-registers the specified extension vault.
Once un-registered, the vault is no longer available to SecretManagement, for the current user.

## EXAMPLES

### Example 1
```powershell
PS C:\> Get-SecretVault

VaultName  ModuleName                        IsDefaultVault
---------  ----------                        --------------
CredMan    Microsoft.PowerShell.CredManStore False
LocalStore Microsoft.PowerShell.SecretStore  True

PS C:\> Unregister-SecretVault LocalStore
PS C:\> Get-SecretVault

VaultName ModuleName                        IsDefaultVault
--------- ----------                        --------------
CredMan   Microsoft.PowerShell.CredManStore False

PS C:\> Get-Secret -Name Secret5
Get-Secret: The secret Secret5 was not found.

PS C:\> Register-SecretVault -Name SecretStore -ModuleName Microsoft.PowerShell.SecretStore -DefaultVault
PS C:\> Get-SecretVault

VaultName   ModuleName                        IsDefaultVault
---------   ----------                        --------------
CredMan     Microsoft.PowerShell.CredManStore False
SecretStore Microsoft.PowerShell.SecretStore  True

PS C:\> Get-Secret -Name Secret5
System.Security.SecureString
```

In this example, 'Get-SecretVault' command is run to see what vaults are registered for the current user.
Next, the 'LocalStore' vault is un-registered.
'Get-SecretVault' command is run again to verify the vault no longer appears in the registry.
An attempt is made to retrieve 'Secret5', but it is not found since its vault was un-registered.
The vault is re-registered, under a different name, and set to be the default vault.
'Get-SecretVault' is run again to verify the newly registered vault.
Finally, the 'Secret5' secret is retrieved successfully from the new default vault.

### Example 2
```powershell
PS C:\> Get-SecretVault | Unregister-SecretVault
PS C:\> Get-SecretVault
PS C:\>
```

In this example, 'Get-SecretVault' output is piped to this 'Unregister-SecretVault' cmdlet to un-register all extension vaults for the current user.
Next, 'Get-SecretVault' is run again to show that no vaults are registered.

## PARAMETERS

### -Name
Name of the vault to un-register.

```yaml
Type: String[]
Parameter Sets: NameParameterSet
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: True
```

### -SecretVault
SecretVaultInfo object, returned by 'Get-SecretVault' cmdlet.
This can alternately be used to indicate a vault to be un-registered.

```yaml
Type: SecretVaultInfo
Parameter Sets: SecretVaultParameterSet
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName, ByValue)
Accept wildcard characters: False
```

### -Confirm
Prompts you for confirmation before running the cmdlet.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf
Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### Microsoft.PowerShell.SecretManagement.SecretVaultInfo
## OUTPUTS

## NOTES

## RELATED LINKS

---
# Set-SecretVaultDefault

## SYNOPSIS
Sets the provided vault name as the default vault for the current user.

## SYNTAX

### NameParameterSet (Default)
```
Set-SecretVaultDefault [-Name] <String> [-WhatIf] [-Confirm] [<CommonParameters>]
```

### SecretVaultParameterSet
```
Set-SecretVaultDefault [-SecretVault] <SecretVaultInfo> [-WhatIf] [-Confirm] [<CommonParameters>]
```

### ClearParameterSet
```
Set-SecretVaultDefault [-ClearDefault] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
This cmdlet updates the vault registry to designate the provided vault name as the default vault.
Only one registered vault can be the default vault.
If this cmdlet is run without specifying the 'Name' parameter, then no registered vault is designated as the default vault.

## EXAMPLES

### Example 1
```powershell
PS C:\> Get-SecretVault

VaultName  ModuleName                        IsDefaultVault
---------  ----------                        --------------
CredMan    Microsoft.PowerShell.CredManStore False
LocalStore Microsoft.PowerShell.SecretStore  True

PS C:\> Set-SecretVaultDefault -Name CredMan
PS C:\> Get-SecretVault

VaultName  ModuleName                        IsDefaultVault
---------  ----------                        --------------
CredMan    Microsoft.PowerShell.CredManStore True
LocalStore Microsoft.PowerShell.SecretStore  False

PS C:\> Set-SecretVaultDefault
PS C:\> Get-SecretVault

VaultName  ModuleName                        IsDefaultVault
---------  ----------                        --------------
CredMan    Microsoft.PowerShell.CredManStore False
LocalStore Microsoft.PowerShell.SecretStore  False
```

This cmdlet first runs 'Get-SecretVault' command to get all registered vault information, and shows that the 'LocalStore' is currently the default vault for the user.
Next, the 'Set-SecretVaultDefault' command is run to make the 'CredMan' vault the default vault.
The 'Get-SecretVault' command is run a second time to verify 'CredMan' vault is now default, and 'LocalStore' vault is no longer default.
Finally, the 'Set-SecretVaultDefault' command is run with no 'Name' parameter, to remove the default designation from any registered vault.
The 'Get-SecretVault' is run once again to verify there is no default vault.

## PARAMETERS

### -ClearDefault
Makes no registered vault the default vault.

```yaml
Type: SwitchParameter
Parameter Sets: ClearParameterSet
Aliases:

Required: False
Position: 0
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
Name of registered vault to be made the default vault.

```yaml
Type: String
Parameter Sets: NameParameterSet
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -SecretVault
A SecretVaultInfo object that represents the registered vault to be made the default vault.

```yaml
Type: SecretVaultInfo
Parameter Sets: SecretVaultParameterSet
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName, ByValue)
Accept wildcard characters: False
```

### -Confirm
Prompts you for confirmation before running the cmdlet.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf
Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

## NOTES

## RELATED LINKS

---
# Test-SecretVault

## SYNOPSIS
Runs an extension vault self test.

## SYNTAX

```
Test-SecretVault [[-Name] <String[]>] [<CommonParameters>]
```

## DESCRIPTION
This cmdlet runs an extension vault self test, by running the internal vault 'Test-SecretVault' command.
It will return 'True' if all tests succeeded, and 'False' otherwise.
Information on failing tests will be written to the error stream as error records.
For more information during the test run use the -Verbose command switch.

## EXAMPLES

### Example 1
```powershell
PS C:\> Test-SecretVault -Name CredMan -Verbose
VERBOSE: Invoking command Test-SecretVault on module Microsoft.PowerShell.CredManStore.Extension
VERBOSE: Vault CredMan succeeded validation test
True
```

This example runs self tests on the 'CredMan' extension vault.
All tests succeeded so no errors are written and 'True' is returned.

## PARAMETERS

### -Name
Name of vault to run self tests on.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName, ByValue)
Accept wildcard characters: True
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### System.Boolean
## NOTES

## RELATED LINKS

---
# Unlock-SecretVault

## SYNOPSIS
Unlocks an extension vault so that it can be access in the current session.

## SYNTAX

```
Unlock-SecretVault [-Name] <String> [-Password] <SecureString> [<CommonParameters>]
```

## DESCRIPTION
This cmdlet unlocks an extension vault using the provided Password parameter argument.
This allows a vault that requires password authentication to operate without first having to prompt
the user.
Not all extension vaults require password authentication, in which case this command has no effect.
A warning will be emitted if the extension vault does not support unlocking via password.

## EXAMPLES

### Example 1
```powershell
PS C:\> Unlock-SecretVault -Name SecretStore -Password $SecurePassword
PS C:\> Get-SecretInfo -Vault SecretStore

Name             Type         VaultName
----             ----         ---------
Secret1          SecureString SecretStore
Secret2          SecureString SecretStore
```

This example uses the command to unlock the SecretStore vault.
It then runs the 'Get-SecretInfo' command on the vault without being prompted for the vault
password.

### Example 2
```powershell
PS C:\> Unlock-SecretVault -Name CredMan -Password $SecurePassword
WARNING: Cannot unlock extension vault 'CredMan': The vault does not support the Unlock-SecretVault function.
PS C:\>
```

This example uses the command to unlock the CredMan vault.
But the vault does not support unlocking so the command has no effect.
A warning is displayed informing that CredMan vault does not support unlocking.

## PARAMETERS

### -Name
Name of the vault to unlock.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Password
Password used to unlock the vault.

```yaml
Type: SecureString
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None

## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS


