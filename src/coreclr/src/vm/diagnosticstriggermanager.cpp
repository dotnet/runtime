// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "diagnosticstriggermanager.h"

DiagnosticsTriggerManager DiagnosticsTriggerManager::s_instance;

/* static */ DiagnosticsTriggerManager& DiagnosticsTriggerManager::GetInstance()
{
    return s_instance;
}

DiagnosticsTriggerManager::DiagnosticsTriggerManager()
{
    this->survivorAnalyzers = nullptr;
}

struct Property
{
    const WCHAR* propertyName;
    const WCHAR* propertyValue;
    struct Property* next;
};
struct Property* ParsePropertyList(const WCHAR* condition);
void DeletePropertyList(struct Property* property);
const WCHAR* GetPropertyValue(struct Property* property, const WCHAR* propertyName);

class PropertyListHolder
{
public:
    PropertyListHolder(struct Property* p)
    {
        this->p = p;
    }
    ~PropertyListHolder()
    {
        DeletePropertyList(this->p);
    }
private:
    struct Property* p;
};

void AndrewDebug()
{

}

bool DiagnosticsTriggerManager::RegisterTrigger(const WCHAR* condition, const WCHAR* identity, IDiagnosticsTriggerAction* action)
{
    struct Property* property = ParsePropertyList(condition);
    if (property == nullptr)
    {
        return false;
    }
    PropertyListHolder h(property);

    NewArrayHolder<WCHAR> copiedIdentity = new (nothrow) WCHAR[wcslen(identity) + 1];
    if (copiedIdentity == nullptr)
    {
        return false;
    }
    wcscpy(copiedIdentity, identity);
    
    const WCHAR* when = GetPropertyValue(property, L"when");
    if (when == nullptr)
    {
        return false;
    }
    if (wcscmp(when, L"ongcmarkcomplete") == 0)
    {
        const WCHAR* genString = GetPropertyValue(property, L"gen");
        const WCHAR* promotedBytesThresholdString = GetPropertyValue(property, L"promoted_bytes_threshold");
        unsigned long gen;
        if (genString == nullptr)
        {
            return false;
        }
        else
        {
            WCHAR* genEnd;
            gen = wcstoul(genString, &genEnd, 10);
            if (*genEnd != '\0')
            {
                return false;
            }
        }
        unsigned long promotedBytesThreshold;
        if (promotedBytesThresholdString == nullptr)
        {
            return false;
        }
        else
        {
            WCHAR* promotedBytesThresholdEnd;
            promotedBytesThreshold = wcstoul(promotedBytesThresholdString, &promotedBytesThresholdEnd, 10);
            if (*promotedBytesThresholdEnd != '\0')
            {
                return false;
            }
        }
        struct SurvivorAnalyzer* temp = new (nothrow) SurvivorAnalyzer();
        if (temp == nullptr)
        {
            return false;
        }
        temp->gen = gen;
        temp->promotedBytesThreshold = promotedBytesThreshold;
        temp->action = action;
        AndrewDebug();
        temp->identity = copiedIdentity.Extract();
        temp->next = this->survivorAnalyzers;
        this->survivorAnalyzers = temp;
        return true;
    }
    else
    {
        // Here is where we can add additional 'when' triggers
        return false;
    }
}

bool DiagnosticsTriggerManager::UnregisterTrigger(const WCHAR* identity)
{
    AndrewDebug();
    struct SurvivorAnalyzer fakeHead;
    fakeHead.next = this->survivorAnalyzers;
    struct SurvivorAnalyzer* prev = &fakeHead;
    struct SurvivorAnalyzer* curr = this->survivorAnalyzers;
    while (curr != nullptr)
    {
        if (wcscmp(identity, curr->identity) == 0)
        {
            prev->next = curr->next;
            curr->action->Cancel();
            delete curr->identity;
            delete curr->action;
            this->survivorAnalyzers = fakeHead.next;
            return true;
        }
        prev = curr;
        curr = curr->next;
    }
    return false;
}

void DiagnosticsTriggerManager::AnalyzeSurvivors()
{
    CONTRACT_VIOLATION(ModeViolation);
    struct SurvivorAnalyzer* survivorAnalyzer = this->survivorAnalyzers;
    while (survivorAnalyzer != nullptr)
    {
        // Be careful, the variable survivorAnalyzer might be come invalid if 
        // the action unregister itself, therefore keeping the next pointer here 
        // first
        struct SurvivorAnalyzer* next = survivorAnalyzer->next;
        // TODO, andrewau, evaluate condition
        survivorAnalyzer->action->Run();
        survivorAnalyzer = next;
    }
}

void DiagnosticsTriggerManager::Shutdown()
{
    CONTRACT_VIOLATION(ModeViolation);
    AndrewDebug();
    struct SurvivorAnalyzer* survivorAnalyzer = this->survivorAnalyzers;
    while (survivorAnalyzer != nullptr)
    {
        struct SurvivorAnalyzer* next = survivorAnalyzer->next;
        survivorAnalyzer->action->Cancel();
        delete survivorAnalyzer;
        survivorAnalyzer = next;
    }
}

const WCHAR* GetPropertyValue(struct Property* property, const WCHAR* propertyName)
{
    while (property != nullptr)
    {
        if (wcscmp(property->propertyName, propertyName) == 0)
        {
            return property->propertyValue;
        }
        property = property->next;
    }
    return nullptr;
}

struct Property* ParsePropertyList(const WCHAR* condition)
{
    const WCHAR* p = condition;
    struct Property* property = nullptr;
    
    const WCHAR* start = nullptr;
    WCHAR* buffer;
    int state = 0;
    bool error = false;

    while (!error)
    {
        if (state == 0)
        {
            switch (*p)
            {
            case '\0':
                p = nullptr;
                break;
            case ' ':
                break;
            case '=':
                error = true;
                break;
            case ',':
                error = true;
                break;
            default:
                state = 1;
                struct Property* temp = new (nothrow) Property();
                if (temp == nullptr)
                {
                    error = true;
                }
                temp->propertyName = nullptr;
                temp->propertyValue = nullptr;
                temp->next = property;
                property = temp;
                start = p;
                break;
            }
        }
        else if (state == 1)
        {
            switch (*p)
            {
            case '\0':
                error = true;
                break;
            case ' ':
            case '=':
                assert(start != nullptr);
                buffer = new (nothrow) WCHAR[p - start + 1];
                if (buffer == nullptr)
                {
                    error = true;
                }
                else
                {
                    assert(property != nullptr);
                    memcpy(buffer, start, sizeof(WCHAR)* (p - start));
                    buffer[p - start] = '\0';
                    property->propertyName = buffer;
                }
                state = (*p) == '=' ? 3 : 2;
                break;
            case ',':
                error = true;
                break;
            default:
                break;
            }
        }
        else if (state == 2)
        {
            switch (*p)
            {
            case '\0':
                error = true;
                break;
            case ' ':
                break;
            case '=':
                state = 3;
                break;
            case ',':
                error = true;
                break;
            default:
                error = true;
                break;
            }
        }
        else if (state == 3)
        {
            switch (*p)
            {
            case '\0':
                error = true;
                break;
            case ' ':
                break;
            case '=':
                error = true;
                break;
            case ',':
                error = true;
                break;
            default:
                state = 4;
                start = p;
                break;
            }
        }
        else if (state == 4)
        {
            switch (*p)
            {
            case '\0':
            case ' ':
            case ',':
                assert(start != nullptr);
                buffer = new (nothrow) WCHAR[p - start + 1];
                if (buffer == nullptr)
                {
                    error = true;
                }
                else
                {
                    memcpy(buffer, start, sizeof(WCHAR)* (p - start));
                    buffer[p - start] = '\0';
                    property->propertyValue = buffer;
                }
                if (*p == '\0')
                {
                    p = nullptr;
                }
                else 
                {
                    state = (*p) == ',' ? 0 : 5;
                }
                break;
            default:
                break;
            }
        }
        else if (state == 5)
        {
            switch (*p)
            {
            case '\0':
                p = nullptr;
                break;
            case ' ':
                break;
            case '=':
                error = true;
                break;
            case ',':
                state = 0;
                break;
            default:
                error = true;
                break;
            }
        }
        if (p == nullptr)
        {
            break;
        }
        p++;
    }
    if (error)
    {
        DeletePropertyList(property);
        property = nullptr;
    }
    return property;
}

void DeletePropertyList(struct Property* property)
{
    while (property != nullptr)
    {
        struct Property* temp = property->next;
        if (property->propertyName != nullptr)
        {
            delete[] property->propertyName;
        }
        if (property->propertyValue != nullptr)
        {
            delete[] property->propertyValue;
        }
        delete property;
        property = temp;
    }
}