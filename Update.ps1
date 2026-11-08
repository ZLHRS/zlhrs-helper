param([int]$WaitForProcess=0,[switch]$NoDialog,[string]$ProjectRoot=$PSScriptRoot)
$ErrorActionPreference='Stop'
$root=(Resolve-Path -LiteralPath $ProjectRoot).ProviderPath
$env:GIT_TERMINAL_PROMPT='0'
$env:GCM_INTERACTIVE='Never'
$env:GIT_SSH_COMMAND='ssh -o BatchMode=yes -o ConnectTimeout=15'
$script:lastGitExit=0
function Invoke-ProjectGit {
    if($root -match '^\\\\(?:wsl\.localhost|wsl\$)\\([^\\]+)\\(.+)$'){
        $distro=$Matches[1]
        $linuxPath='/'+$Matches[2].Replace('\','/')
        $output=& "$env:WINDIR\System32\wsl.exe" -d $distro --cd $linuxPath --exec env GIT_TERMINAL_PROMPT=0 GCM_INTERACTIVE=Never 'GIT_SSH_COMMAND=ssh -o BatchMode=yes -o ConnectTimeout=15' git @args
    }else{
        $git=Get-Command git.exe -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
        if(-not $git){throw 'Для обновления установите Git for Windows.'}
        $output=& $git.Source -C $root @args
    }
    $script:lastGitExit=$LASTEXITCODE
    return $output
}
function Show-UpdateResult([string]$message,[bool]$failed) {
    if($NoDialog){Write-Output $message;return}
    Add-Type -AssemblyName System.Windows.Forms
    $icon=if($failed){'Warning'}else{'Information'}
    [void][Windows.Forms.MessageBox]::Show($message,'Обновление ZLHRS Helper','OK',$icon)
}
try {
    if($WaitForProcess -gt 0){
        $old=Get-Process -Id $WaitForProcess -ErrorAction SilentlyContinue
        if($old){Wait-Process -InputObject $old -Timeout 20}
    }
    if(-not (Test-Path -LiteralPath (Join-Path $root '.git'))){throw 'Для обновления нужна папка из git clone.'}
    $origin=(Invoke-ProjectGit remote get-url origin | Out-String).Trim()
    if($script:lastGitExit -ne 0 -or $origin -notmatch '^(?:git@github\.com:|ssh://git@github\.com/|https://github\.com/)ZLHRS/zlhrs-helper(?:\.git)?$'){
        throw 'Обновление доступно только для origin ZLHRS/zlhrs-helper на GitHub.'
    }
    $branch=(Invoke-ProjectGit branch --show-current | Out-String).Trim()
    if($script:lastGitExit -ne 0 -or $branch -ne 'main'){throw 'Для обновления переключитесь на ветку main.'}
    $status=Invoke-ProjectGit status --porcelain
    if($script:lastGitExit -ne 0 -or $status){throw 'В проекте есть локальные изменения. Сохраните их перед обновлением.'}
    Invoke-ProjectGit fetch --no-recurse-submodules origin main | Out-Null
    if($script:lastGitExit -ne 0){throw 'Не удалось получить обновление. Проверьте интернет и доступ Git к приватному репозиторию.'}
    $local=(Invoke-ProjectGit rev-parse HEAD | Out-String).Trim()
    $remote=(Invoke-ProjectGit rev-parse FETCH_HEAD | Out-String).Trim()
    if($script:lastGitExit -ne 0 -or $remote -notmatch '^[0-9a-f]{40,64}$'){throw 'Не удалось проверить полученный коммит.'}
    if($local -eq $remote){Show-UpdateResult 'У вас уже последнее обновление. Откройте приложение снова.' $false;exit 0}
    Invoke-ProjectGit merge-base --is-ancestor HEAD $remote | Out-Null
    if($script:lastGitExit -ne 0){throw 'Ветка отличается от GitHub. Автоматическое обновление остановлено.'}
    Invoke-ProjectGit -c core.hooksPath= merge --ff-only $remote | Out-Null
    if($script:lastGitExit -ne 0){throw 'Не удалось применить обновление. Проверьте, закрыты ли другие экземпляры приложения.'}
    Show-UpdateResult 'Обновление установлено. Откройте приложение тем же ярлыком.' $false
    exit 0
}catch {
    Show-UpdateResult $_.Exception.Message $true
    exit 1
}
