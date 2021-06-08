# Pop3cli

[![Build status]][appveyor]
[![GitHub Release]][releases]

Консольный POP3 клиент для получения всех вложений.

Программа требует установленного .NET Framework 4.8.
Настройки в файле `.exe.config`.

Программа подключается к POP3 серверу MOEX,
получает список писем (сервер сам их подчищает),
скачивает недостающее на локальном диске,
извлекает все вложения в указанную папку.
Локальные письма, которые уже удалены с сервера,
переносятся в папку BAK.

## Extra

Далее с этими вложениями работает скрипт `moexload.cmd`
(пример в папке `extra`),
который удаляет все наложенные в любом порядке шифрования,
подписи и упаковки (`.p7a`, `.p7e`, `.p7s`, `.zip`),
оставляя чистые XML и PDF для импорта в разные системы.
Скрипт использует утилиту командной строки СКЗИ Валидата
`xpki1utl.exe`.

## License

Licensed under the [Apache License, Version 2.0].

[Apache License, Version 2.0]: http://www.apache.org/licenses/LICENSE-2.0 "LICENSE"

[appveyor]: https://ci.appveyor.com/project/diev/pop3cli
[releases]: https://github.com/diev/Pop3cli/releases/latest

[Build status]: https://ci.appveyor.com/api/projects/status/t7hsyhlqq970y9vs?svg=true
[GitHub Release]: https://img.shields.io/github/release/diev/Pop3cli.svg
