# SpawnDev.BlazorJS.FromTypeScript
A tool for creating SpawnDev.BlazorJS.JSObject interop libraires from TypeScript Declarations files `*.d.ts` 

[FromTypeScript Web App](https://lostbeard.github.io/SpawnDev.BlazorJS.FromTypeScript/)


## Demo usage
- Click `File` > `Import *.d.ts Zip`
- Select zip downloaded from [three-types/three-ts-types](https://github.com/three-types/three-ts-types/archive/refs/heads/master.zip)
- Enter `THREE` into the `Import as?` prompt.
- Wait (no progress indicator atm.)
- Get notification of completed import.
- Should see `Typescript` root folder and its sub-folder `THREE` populate with the typescript declaration files. (May need to collapse, expand the tree view.)
- Right click the `TypeScript > THREE > src` folder and select `Extract JSObjects`
- Enter a `Project name` like `Blazor.THREE` and click `Create`
- Wait (no progress indicator atm.)
- Should see `Blazor` root folder and its sub-folder `Blazor.THREE` populate with the JSObject files. (May need to collapse, expand the tree view.)

Currently a bit of a mess, but I'll clean it up and improve the output (and progress indicators.)


![FromTypeScript](https://raw.githubusercontent.com/LostBeard/SpawnDev.BlazorJS.FromTypeScript/master/SpawnDev.BlazorJS.FromTypeScript/wwwroot/screenshots/FromTypeScript2.jpg)
