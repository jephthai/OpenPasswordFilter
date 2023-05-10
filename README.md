Introduction
------------
OpenPasswordFilter is an open source custom password filter DLL and userspace service to better protect / control
Active Directory domain passwords. It is a an update of  jephthai/OpenPasswordFilter. This version uses a much faster
and more efficient matching system, to check a large list of commopn (disallowed) passwords very quickly. It also has an updated
installer.

--
A new approach introduced at Defcon 2022 has produced better results in testing.
For the new approach, see [https://github.com/sensei-hacker/password-dog](https://github.com/sensei-hacker/password-dog)
--

The genesis of this idea comes from conducting many penetration tests where organizations have users who choose common passwords
and the ultimate difficulty of controlling this behavior.  The fact is that any domain of size will have some user who chose
`Password1` or `Summer2015` or `Company123` as their password.  Any intruder or low-privilege user who can guess or obtain
usernames for the domain can easily run through these very common passwords and start expanding the level of access in the 
domain.

Microsoft provides a wonderful feature in Active Directory, which is the ability to create a custom password filter DLL.  This
DLL is loaded by LSASS on boot (if configured), and will be queried for each new password users attempt to set.  The DLL simply
replies with a `TRUE` or `FALSE`, as appropriate, to indicate that the password passes or fails the test.  

There are some commercial options, but they are usually in the "call for pricing" category, and that makes it a little 
prohibitive for some organizations to implement truly effective preventive controls for this class of very common bad passwords. 

This is where OpenPasswordFilter comes in -- an open source solution to add basic dictionary-based rejection of common
passwords.

OPF is comprised of two main parts:

   1. OpenPasswordFilter.dll -- this is a custom password filter DLL that can be loaded by LSASS to vet incoming password changes.
   2. PasswordCheckerRay.exe -- this is a C#-based service binary that provides a local user-space service for maintaining the dictionary and servicing requests.
  
The DLL communicates with the service on the loopback network interface to check passwords against the configured database
of forbidden values.  This architecture is selected because it is difficult to reload the DLL after boot, and administrators
are likely loathe to reboot their DCs when they want to add another forbidden password to the list.  Just bear in mind how this
architecture works so you understand what's going on.

**NOTE** The current version is very ALPHA!  I have tested it on some of my DCs, but your mileage may vary and you may wish to
test in a safe location before using this in real life.

Installation
------------
You can download a precompiled 64-bit version of OPF from the following link:

[OpenPasswordFilter-release.zip](https://github.com/MorrisR2/OpenPasswordFilter/raw/master/OpenPasswordFilter-release.zip)

Just run the installer in that zip file, then reboot.

The installer does a couple of things in addition to putting the files in place.
It configures the DLL so that Windows will load it for filtering passwords.  Note that this needs to be se tup on all
on all domain controllers, as any of them may end up servicing a password change request. If for some reason you need to 
do it manually rather thanusing the the incolude installer, here is a link to Microsoft's
documentation for setting up a password filter:

    https://msdn.microsoft.com/en-us/library/windows/desktop/ms721766(v=vs.85).aspx
    
The bottom line is this:

  1. Copy `OpenPasswordFilter.dll` to `%WINDIR%\System32`
  2. Configure the `HKLM\SYSTEM\CurrentControlSet\Control\Lsa\Notification Packages` registry key with the DLL name
  
Note, you do not include the `.dll` extension in the registry key -- just `OpenPasswordFilter`.

Next, you will want to configure the OPF service (unless you use the installer).  You can do so as follows:

    > sc create OPF binPath= "<full path to exe>\PasswordCheckerRay.exe" start=boot

The system can use two dictionary files in the same directory where you installed PasswordChckerRay.exe, named
`opfmatch.txt`and `custom_sha1.txt`. 

For generic common passwords, simply add them to this file:
C:\Program Files\Confie Infosec\PasswordChecker\data\opfmatch.txt
Note this process leaves the disallowed passwords readable in plain text, so it should only be used for
generic passwords like “Password123!”

Passwords in `opfmatch.txt` will be tested for matches. The provided default list if the top million most
commonly-used passwords. One could also use hashcat to make a list.  Notice the list is al lower case.
OpenPasswordFilter will disallow these passwords regardless of casing used.  That is, if you disallow
"password123", that will also automatically apply to "Password123" and "PASSWORD123".

=Company-specific passwords that may be in use on your network=

Passwords that may actually be in use on your network, such as default passwords that many people know, can be
be handled differently. To disallow future re-use of a password that may currently be in use, a hash of the
password needs to be added to a different file.

To add custom disallowed passwords that may currently be in use, they can be added to the following file:

C:\Program Files\Confie Infosec\PasswordChecker\custom_sha256.txt
Each line in the file should the SHA-256 of the lowercased version of the password, which can be obtained with
the following Powershell:

$clearString = "C0mpany1!" # Replace C0mpany1! With the password you want to disallow
$hasher = [System.Security.Cryptography.HashAlgorithm]::Create('sha256')
$hash = $hasher.ComputeHash( [System.Text.Encoding]::UTF8.GetBytes( $ClearString.ToLower() ) )
[System.BitConverter]::ToString($hash).Replace('-', '').toLower() 

This is the appropriate way to company-specific disallowed passwords like “C0mpany2021!”
The SHA256 hash is used in case you want to ban further use a password that is already used in the company. This will
prevent the banned password from being used on new accounts, or having the password of an existing account changed to
the banned password.  Using the hash avoids making the actual password available to anyone who might read the file. 


Bear in mind that if you use a unix like system to create your wordlists, the line terminators will need changing to
Windows format:

`unix2dos opfmatch.txt`

If the service fails to start, it's likely an error ingesting the wordlists, and the line number of the problem entry will be
written to the Application event log.

Or you can skip all this and use the installer. You WILL need to manually reboot adfter running the installer.

If all has gone well, reboot your DC and test by using the normal GUI password reset function to choose a password that is on
your forbidden list.


