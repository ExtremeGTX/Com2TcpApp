[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]


<!-- PROJECT LOGO -->
<br />
<div align="center">
  <a href="https://github.com/ExtremeGTX/Com2TcpApp">
    <img src="docs/icons8-left-right-96.png" alt="Logo" width="80" height="80">
  </a>

<h3 align="center">com2tcp</h3>

  <p align="center">
    A simple command line tool to forward data between a COM Port and a TCP socket
    <br />
    <a href="https://github.com/ExtremeGTX/Com2TcpApp"><strong>Explore the docs »</strong></a>
    <br />
    <br />
    <a href="https://github.com/ExtremeGTX/Com2TcpApp">View Demo</a>
    ·
    <a href="https://github.com/ExtremeGTX/Com2TcpApp/issues">Report Bug</a>
    ·
    <a href="https://github.com/ExtremeGTX/Com2TcpApp/issues">Request Feature</a>
  </p>
</div>


<!-- ABOUT THE PROJECT -->
## About The Project

  A simple command line tool to forward data between a COM Port and a TCP socket


## Features
- Forward data between a COM Port and a TCP socket
- Automatic recovery if COM Port is removed and re-connected
- Export the COM port to WSL

### Built With

* [![.NET]][.NET-url]
* [![vscode-shield]][VS-url]
* [![Windows]][Windows-url]


<!-- GETTING STARTED -->
## Getting Started

### Prerequisites

- [.NET Core 6.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-aspnetcore-6.0.16-windows-x64-installer?cid=getdotnetcore)

### Installation
- Extract the zip archive and run, see the Usage section below.

<!-- USAGE EXAMPLES -->
## Usage

- Basic usage:
  `com2tcp.exe --com-port COM1`

- Specify Adapter address and port
  `com2tcp.exe --com-port COM1 --tcp-address 192.168.1.2 --tcp-port 5001`

- Forward the COM port on the WSL adapter
  `com2tcp.exe --com-port COM1 --tcp-port 5001 --wsl`

<!-- LICENSE -->
## License

Distributed under the MIT License. See `LICENSE.txt` for more information.


<!-- CONTACT -->
## Contact

Mohamed ElShahawi - [@extremegtx](https://twitter.com/extremegtx)

Project Link: [https://github.com/ExtremeGTX/Com2TcpApp](https://github.com/ExtremeGTX/Com2TcpApp)


<!-- ACKNOWLEDGMENTS -->
## Acknowledgments

* [Best-README-Template](https://github.com/othneildrew/Best-README-Template/)
* Project icon from [icons8](https://icons8.com/icons/set/usb)


<!-- MARKDOWN LINKS & IMAGES -->
<!-- https://www.markdownguide.org/basic-syntax/#reference-style-links -->
[contributors-shield]: https://img.shields.io/github/contributors/ExtremeGTX/USBWatcher.svg?style=for-the-badge
[contributors-url]: https://github.com/ExtremeGTX/Com2TcpApp/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/ExtremeGTX/USBWatcher.svg?style=for-the-badge
[forks-url]: https://github.com/ExtremeGTX/Com2TcpApp/network/members
[stars-shield]: https://img.shields.io/github/stars/ExtremeGTX/USBWatcher.svg?style=for-the-badge
[stars-url]: https://github.com/ExtremeGTX/Com2TcpApp/stargazers
[issues-shield]: https://img.shields.io/github/issues/ExtremeGTX/USBWatcher.svg?style=for-the-badge
[issues-url]: https://github.com/ExtremeGTX/Com2TcpApp/issues
[license-shield]: https://img.shields.io/github/license/ExtremeGTX/USBWatcher.svg?style=for-the-badge
[license-url]: https://github.com/ExtremeGTX/Com2TcpApp/blob/master/LICENSE.txt
[product-screenshot]: docs/USBWatcher_Screenshot.png
[VS-url]: https://code.visualstudio.com/
[.NET-url]: https://dotnet.microsoft.com/en-us/download/dotnet-framework
[Windows-url]: https://www.microsoft.com/en-us/windows
[.NET]: https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white
[vscode-shield]: https://img.shields.io/badge/Visual%20Studio%20Code-0078d7.svg?style=for-the-badge&logo=visual-studio-code&logoColor=white
[Windows]: https://img.shields.io/badge/Windows-0078D6?style=for-the-badge&logo=windows&logoColor=white