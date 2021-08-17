# [Pop3cli]

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

## Параметры запуска

0: Читать файл конфигурации и, если указан `Host`,
загрузить файлы с этого POP3 сервера,
если нет - просто распаковать все вложения из файлов
по маске `Src` в папку `Dst`.

1: Если `-?` или `/h` - показать эту помощь,
иначе - по указанной маске вместо `Src` в конфиге.

2: Использовать указанные параметры вместо `Src` и `Dst`
в конфиге.

(Маска `Src` - это просто имя директории или
имя директории с маской файлов или именем файла в ней.)

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

[Pop3cli]: https://diev.github.io/Pop3cli/
[Apache License, Version 2.0]: LICENSE

[appveyor]: https://ci.appveyor.com/project/diev/pop3cli
[releases]: https://github.com/diev/Pop3cli/releases/latest

[Build status]: https://ci.appveyor.com/api/projects/status/t7hsyhlqq970y9vs?svg=true
[GitHub Release]: https://img.shields.io/github/release/diev/Pop3cli.svg
