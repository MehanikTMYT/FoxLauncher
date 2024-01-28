# FoxLauncher
Этот лаунчер написан на WPF C# в Visual Studio 2022 с использованием пакета Cmllib.core, документация - https://alphabs.gitbook.io/cmllib/cmllib.core/cmllib.core

В лаунчере имеется БАЗОВЫЙ функционал включающий в себя загрузку списка клиентов, самих клиентов и требуемых файлом (если нет папки клиента не важно какой режим выбрать) 

Интерфейс главного окна если не выбран клиент


<img width="264" alt="image" src="https://github.com/MehanikTMYT/FoxLauncher/assets/29605858/d85bd171-3106-4a99-b4fb-85f8337bdfe3">


Интерфейс главного окна если выбран клиент


<img width="263" alt="image" src="https://github.com/MehanikTMYT/FoxLauncher/assets/29605858/e495e068-668f-4c1a-8f2d-a3a7dae99163">

Окно настроек


<img width="264" alt="image" src="https://github.com/MehanikTMYT/FoxLauncher/assets/29605858/7a0b16e1-5fb8-4edf-88c0-31d26799f9f2">

При первом запуске (или если сброшен путь) лаунчер спрашивает путь для клиетов игры

Окно клиентов игры

<img width="264" alt="image" src="https://github.com/MehanikTMYT/FoxLauncher/assets/29605858/fedfde17-83ca-4de5-aac1-7690e78428fa">

Все клиенты считываются из текстового файла clients.txt (он скачиватся каждый раз при запуске лаунчера)

Окно загрузки клиента 


<img width="260" alt="image" src="https://github.com/MehanikTMYT/FoxLauncher/assets/29605858/7b1a874f-a2bd-4070-8c92-722bd8a28306">

Для работы лаунчера нужно изменить в строчке *string url = "ВВЕДИТЕ АДРЕС ЗАГРЗУЗКИ";* адрес загрузки с которого будут происходить скачивания. Самый удобный способ это скачивать с VDS на котором настроен nginx ибо с этим способом не нужно менять код. 

*ВСЕ ЗНАЧЕНИЯ ОЗУ, ПУТИ КЛИЕТОВ, ВЫБРАННОГО КЛИЕНТА И НИКНЕЙМА СОХРАНЯЮТСЯ В ПАМЯТИ ПРОГРАММЫ И БУДУТ ВСЕГДА ЗАПОЛНЯТЬСЯ ПОСЛЕДНИМИ ЗНАЧЕНИЯМИ ПРИ КАЖДОМ ЗАПУСКЕ*
