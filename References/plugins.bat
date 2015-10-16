set tmp=%1
set proj=%tmp:Public=%

set plugdir=Plugins
if not exist %plugdir% (
	md %plugdir%
	)

copy StockSharp.Hydra.%proj%.dll %plugdir%\StockSharp.Hydra.%proj%.dll

if %2 == Debug goto :debug

goto :exit

:debug
copy StockSharp.Hydra.%proj%.pdb %plugdir%\StockSharp.Hydra.%proj%.pdb

:exit