# Verifies static layout/contrast invariants; does not replace visual review.
$ErrorActionPreference = 'Stop'
[xml]$shell = Get-Content -LiteralPath 'MainWindow.xaml' -Raw
[xml]$theme = Get-Content -LiteralPath 'App.xaml' -Raw
$names = New-Object System.Xml.XmlNamespaceManager($shell.NameTable)
$names.AddNamespace('wpf', 'http://schemas.microsoft.com/winfx/2006/xaml/presentation')
$nav = $shell.SelectSingleNode("//wpf:Style[@*[local-name()='Key']='ShellTabs']", $names)
if ($null -eq $nav) { throw 'Navigation style not found' }
if ($null -ne $nav.SelectSingleNode('.//wpf:TabPanel', $names)) { throw 'TabPanel can reflow selected tabs' }
if ($null -eq $nav.SelectSingleNode('.//wpf:ItemsPanelTemplate/wpf:StackPanel', $names)) { throw 'Fixed vertical nav missing' }
if ($null -eq $nav.SelectSingleNode('.//wpf:ItemsPresenter', $names)) { throw 'ItemsPresenter missing' }
$items = $shell.SelectNodes('//wpf:TabItem', $names)
if ($items.Count -ne 4) { throw "Unexpected navigation count: $($items.Count)" }
$button = $theme.SelectSingleNode("//wpf:ControlTemplate[@*[local-name()='Key']='HighContrastButtonTemplate']", $names)
if ($null -eq $button) { throw 'High-contrast button template missing' }
if ($null -eq $button.SelectSingleNode('.//wpf:TextBlock[@Foreground="{TemplateBinding Foreground}"]', $names)) {
    throw 'Button labels must explicitly inherit the control foreground'
}
$primary = $theme.SelectSingleNode("//wpf:Style[@*[local-name()='Key']='PrimaryButton']", $names)
if ($null -eq $primary) { throw 'Primary button theme missing' }
$navItem = $shell.SelectSingleNode("//wpf:Style[@*[local-name()='Key']='NavItem']", $names)
if ($null -eq $navItem.SelectSingleNode('./wpf:Setter[@Property="Height"]', $names)) {
    throw 'Navigation rows must have fixed height'
}
Write-Host 'UI contract passed: static sidebar; four fixed-height items; explicit button foreground.'
