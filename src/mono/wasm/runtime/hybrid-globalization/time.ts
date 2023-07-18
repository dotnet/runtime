import { wrap_error_root, wrap_no_error_root } from "../invoke-js";
import { mono_wasm_new_external_root } from "../roots";
import { monoStringToString, stringToUTF16 } from "../strings";
import { Int32Ptr } from "../types/emscripten";
import { MonoObject, MonoObjectRef, MonoString, MonoStringRef } from "../types/internal";
import { OUTER_SEPARATOR } from "./helpers";

export function mono_wasm_get_culture_info(culture: MonoStringRef, dst: number, dstLength: number, isException: Int32Ptr, exAddress: MonoObjectRef): number
{
    const cultureRoot = mono_wasm_new_external_root<MonoString>(culture),
        exceptionRoot = mono_wasm_new_external_root<MonoObject>(exAddress);
    try {
        const cultureName = monoStringToString(cultureRoot);
        const locale = cultureName ? cultureName : undefined;
        const cultureInfo = {
            AmDesignator: "",
            PmDesignator: "",
        };
        const designators = getAmPmDesignators(locale);
        cultureInfo.AmDesignator = designators.am;
        cultureInfo.PmDesignator = designators.pm;
        const result = Object.values(cultureInfo).join(OUTER_SEPARATOR);
        if (result.length > dstLength)
        {
            throw new Error(`Calendar info exceeds length of ${dstLength}.`);
        }
        stringToUTF16(dst, dst + 2 * result.length, result);
        wrap_no_error_root(isException, exceptionRoot);
        return result.length;
    }
    catch (ex: any) {
        wrap_error_root(isException, ex, exceptionRoot);
        return -1;
    }
    finally {
        cultureRoot.release();
        exceptionRoot.release();
    }
}

function getAmPmDesignators(locale: any)
{
    const pmTime = new Date("August 19, 1975 12:15:30");
    const amTime = new Date("August 19, 1975 11:15:30");
    const pmDesignator = getDesignator(pmTime, locale);
    const amDesignator = getDesignator(amTime, locale);
    return {
        am: amDesignator,
        pm: pmDesignator
    };
}
function getDesignator(time: Date, locale: string)
{
    const withDesignator = time.toLocaleTimeString(locale, { hourCycle: "h12"});
    const withoutDesignator = time.toLocaleTimeString(locale, { hourCycle: "h24"});
    const designator = withDesignator.replace(withoutDesignator, "").trim();
    if (new RegExp("[0-9]$").test(designator)){
        // incorrect, take withDesignator, split and choose the part without numbers
        const designatorLikeParts = withDesignator.split(" ").filter(part => new RegExp("^((?![0-9]).)*$").test(part));
        if (!designatorLikeParts || designatorLikeParts.length == 0)
            return "";
        return designatorLikeParts.join(" ");
    }
    return designator;
}